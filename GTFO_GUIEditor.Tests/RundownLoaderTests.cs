using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class RundownLoaderTests
{
    [Fact]
    public void RundownLoader_Instance_IsNotNullAndImplementsInterface()
    {
        IRundownLoader loader = RundownLoader.Instance;
        Assert.NotNull(loader);
    }

    [Fact]
    public void ExportProject_CreatesMinimalRepresentationWithoutDataBlockRedundancy()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);
        var loader = RundownLoader.Instance;

        var project = loader.ExportProject(vm);

        Assert.NotNull(project);
        Assert.Equal(1, project.FileVersion);
        Assert.NotNull(project.Rundown);
        Assert.NotNull(project.Alarms);
        Assert.NotNull(project.Lighting);
        Assert.NotNull(project.FogSettings);
        Assert.NotNull(project.ConsumableDistributions);
        Assert.NotNull(project.Levels);
        Assert.NotEmpty(project.Levels);
    }

    [Fact]
    public void SaveAndLoad_Roundtrip_PreservesAllViewModelDataAccurately()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"rundown_loader_test_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel(autoReopenLastRundown: false);
            vm.Rundown.SelectedRundown = new RundownBlock
            {
                Name = "Rundown Loader Unit Test",
                PersistentId = 42,
                ReqToReachTierB = new TierRequirements { MainSectors = 3, SecondarySectors = 2 }
            };

            var customLevel = new LevelLayoutBlock
            {
                Name = "Deep Underground Complex",
                PersistentId = 999,
                ComplexType = ComplexType.Tech,
                ElevatorTile = "Assets/Prefabs/elevator.prefab"
            };

            customLevel.MainSector.ObjectiveType = ObjectiveType.SpecialTerminalCommand;
            customLevel.MainSector.SpecialTerminalCommand = "DISENGAGE_SECURITY";
            customLevel.MainSector.SpecialTerminalCommandDesc = "Disengages security subsystem";
            customLevel.MainSector.PostCommandOutput = "SYSTEM OVERRIDE\nACCESS GRANTED";
            customLevel.MainSector.ArtifactAmountMultiplier = 2.5;

            customLevel.SecondarySector.ObjectiveType = ObjectiveType.GatherSmallItems;
            customLevel.SecondarySector.GatherItem = GatherSmallItemType.HardDrive;

            vm.LevelLayout.Levels.Clear();
            vm.LevelLayout.Levels.Add(customLevel);

            var loader = RundownLoader.Instance;
            loader.SaveProject(vm, tempFile);

            Assert.True(File.Exists(tempFile));

            var loadedProject = loader.LoadProject(tempFile);
            var loadedVm = new MainWindowViewModel(autoReopenLastRundown: false);
            loader.ApplyProject(loadedVm, loadedProject, tempFile);

            Assert.Equal(tempFile, loadedVm.CurrentFilePath);
            Assert.Equal("Rundown Loader Unit Test", loadedVm.Rundown.SelectedRundown?.Name);
            Assert.Equal((uint)42, loadedVm.Rundown.SelectedRundown?.PersistentId);
            Assert.Equal(3, loadedVm.Rundown.SelectedRundown?.ReqToReachTierB?.MainSectors);

            Assert.Single(loadedVm.LevelLayout.Levels);
            var loadedLvl = loadedVm.LevelLayout.Levels[0];
            Assert.Equal("Deep Underground Complex", loadedLvl.Name);
            Assert.Equal((uint)999, loadedLvl.PersistentId);
            Assert.Equal(ComplexType.Tech, loadedLvl.ComplexType);
            Assert.Equal("DISENGAGE_SECURITY", loadedLvl.MainSector.SpecialTerminalCommand);
            Assert.Equal("Disengages security subsystem", loadedLvl.MainSector.SpecialTerminalCommandDesc);
            Assert.Equal("SYSTEM OVERRIDE\nACCESS GRANTED", loadedLvl.MainSector.PostCommandOutput);
            Assert.Equal(2.5, loadedLvl.MainSector.ArtifactAmountMultiplier);

            Assert.Equal(ObjectiveType.GatherSmallItems, loadedLvl.SecondarySector.ObjectiveType);
            Assert.Equal(GatherSmallItemType.HardDrive, loadedLvl.SecondarySector.GatherItem);
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
    public void LoadProject_ThrowsFileNotFoundException_WhenFileDoesNotExist()
    {
        var loader = RundownLoader.Instance;
        string nonExistentPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.rundown");

        Assert.Throws<FileNotFoundException>(() => loader.LoadProject(nonExistentPath));
    }

    [Fact]
    public void SaveProject_ThrowsArgumentException_WhenPathIsInvalid()
    {
        var loader = RundownLoader.Instance;
        var project = new RundownProjectFile();

        Assert.Throws<ArgumentException>(() => loader.SaveProject(project, ""));
        Assert.Throws<ArgumentException>(() => loader.SaveProject(project, "   "));
    }

    [Fact]
    public void RecentFiles_MaintainsHistoryAndCapsAtTenEntries()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"RecentFiles_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string tempConfig = Path.Combine(tempDir, "recent_files.json");

        try
        {
            RundownLoader.CustomRecentFilesConfigPath = tempConfig;
            var loader = RundownLoader.Instance;

            for (int i = 0; i < 15; i++)
            {
                string path = Path.Combine(tempDir, $"file_{i}.rundown");
                File.WriteAllText(path, "{}");
                loader.AddRecentFile(path);
            }

            var recentFiles = loader.LoadRecentFiles();
            Assert.NotNull(recentFiles);
            Assert.Equal(10, recentFiles.Count);
            Assert.Equal(Path.Combine(tempDir, "file_14.rundown"), recentFiles[0].FilePath);
        }
        finally
        {
            RundownLoader.CustomRecentFilesConfigPath = null;
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
