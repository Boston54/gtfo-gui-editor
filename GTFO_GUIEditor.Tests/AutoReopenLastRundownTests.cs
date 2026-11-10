using System;
using System.IO;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace GTFO_GUIEditor.Tests;

[Collection("RecentFiles")]
public class AutoReopenLastRundownTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _recentFilesPath;
    private readonly string _settingsPath;

    public AutoReopenLastRundownTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"AutoReopenTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _recentFilesPath = Path.Combine(_tempDir, "recent_files.json");
        _settingsPath = Path.Combine(_tempDir, "settings.json");
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;
        EditorSettingsManager.CustomSettingsConfigPath = _settingsPath;
        EditorSettingsManager.Instance.ReloadSettings();
    }

    public void Dispose()
    {
        RundownProjectService.CustomRecentFilesConfigPath = null;
        EditorSettingsManager.CustomSettingsConfigPath = null;
        EditorSettingsManager.Instance.ReloadSettings();
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void MainWindowViewModel_AutoReopensLastRundown_WhenRecentFileExists()
    {
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;

        // Arrange: Create and save a custom rundown file
        string rundownFile = Path.Combine(_tempDir, "MyCustomRundown.rundown");
        var initialVm = new MainWindowViewModel(autoReopenLastRundown: false);
        Assert.NotNull(initialVm.Rundown.SelectedRundown);
        initialVm.Rundown.SelectedRundown.Name = "Auto Reopened Rundown Test";
        var customLevel = new LevelLayoutBlock
        {
            Name = "L1_Special_Level",
            PersistentId = 12345
        };
        initialVm.LevelLayout.Levels.Clear();
        initialVm.LevelLayout.Levels.Add(customLevel);
        initialVm.LevelLayout.SelectedLevel = customLevel;

        RundownProjectService.SaveProject(initialVm, rundownFile);
        RundownProjectService.AddRecentFile(rundownFile);

        // Act: Initialize a new MainWindowViewModel with auto reopen enabled
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;
        var newVm = new MainWindowViewModel(autoReopenLastRundown: true);

        // Assert
        Assert.Equal(rundownFile, newVm.CurrentFilePath);
        Assert.Equal("MyCustomRundown.rundown", newVm.CurrentFileTitle);
        Assert.Equal("Auto Reopened Rundown Test", newVm.Rundown.SelectedRundown?.Name);
        Assert.Single(newVm.LevelLayout.Levels);
        Assert.Equal("L1_Special_Level", newVm.LevelLayout.SelectedLevel?.Name);
        Assert.Contains("Reopened", newVm.StatusMessage);
        Assert.False(newVm.IsDirty);
        Assert.False(newVm.HasUnsavedChanges);
    }

    [Fact]
    public void MainWindowViewModel_DoesNotAutoReopen_WhenAutoReopenIsDisabled()
    {
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;

        // Arrange: Create and save a custom rundown file
        string rundownFile = Path.Combine(_tempDir, "MyCustomRundown.rundown");
        var initialVm = new MainWindowViewModel(autoReopenLastRundown: false);
        Assert.NotNull(initialVm.Rundown.SelectedRundown);
        initialVm.Rundown.SelectedRundown.Name = "Disabled Auto Reopen Test";
        RundownProjectService.SaveProject(initialVm, rundownFile);
        RundownProjectService.AddRecentFile(rundownFile);

        // Act: Initialize with autoReopenLastRundown: false
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;
        var newVm = new MainWindowViewModel(autoReopenLastRundown: false);

        // Assert: Should remain untitled and fresh
        Assert.Null(newVm.CurrentFilePath);
        Assert.Equal("Untitled Rundown", newVm.CurrentFileTitle);
        Assert.Equal("New Rundown", newVm.Rundown.SelectedRundown?.Name);
    }

    [Fact]
    public void MainWindowViewModel_HandlesMissingRecentFileGracefully()
    {
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;

        // Arrange: Add a recent file entry that does not exist on disk
        string nonExistentPath = Path.Combine(_tempDir, "DeletedRundown.rundown");
        RundownProjectService.AddRecentFile(nonExistentPath);

        // Act
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;
        var newVm = new MainWindowViewModel(autoReopenLastRundown: true);

        // Assert: Stays default without throwing
        Assert.Null(newVm.CurrentFilePath);
        Assert.Equal("Untitled Rundown", newVm.CurrentFileTitle);
        Assert.Equal("New Rundown", newVm.Rundown.SelectedRundown?.Name);
    }

    [Fact]
    public void MainWindowViewModel_HandlesCorruptedRecentFileGracefully()
    {
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;

        // Arrange: Create corrupted JSON file and add to recent files
        string corruptedFile = Path.Combine(_tempDir, "Corrupted.rundown");
        File.WriteAllText(corruptedFile, "{ INVALID JSON content }");
        RundownProjectService.AddRecentFile(corruptedFile);

        // Act
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;
        var newVm = new MainWindowViewModel(autoReopenLastRundown: true);

        // Assert: Catches error, sets status message, does not crash
        Assert.Null(newVm.CurrentFilePath);
        Assert.Contains("Failed to reopen last rundown", newVm.StatusMessage);
    }

    [Fact]
    public void TryReopenLastRundown_ReopensMostRecentExistingFile()
    {
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;

        // Arrange: Create two rundown files
        string file1 = Path.Combine(_tempDir, "Older.rundown");
        string file2 = Path.Combine(_tempDir, "Newer.rundown");

        var vm1 = new MainWindowViewModel(autoReopenLastRundown: false);
        Assert.NotNull(vm1.Rundown.SelectedRundown);
        vm1.Rundown.SelectedRundown.Name = "Older Project";
        RundownProjectService.SaveProject(vm1, file1);
        RundownProjectService.AddRecentFile(file1);

        var vm2 = new MainWindowViewModel(autoReopenLastRundown: false);
        Assert.NotNull(vm2.Rundown.SelectedRundown);
        vm2.Rundown.SelectedRundown.Name = "Newer Project";
        RundownProjectService.SaveProject(vm2, file2);
        RundownProjectService.AddRecentFile(file2);

        // Act
        RundownProjectService.CustomRecentFilesConfigPath = _recentFilesPath;
        var vmTest = new MainWindowViewModel(autoReopenLastRundown: false);
        bool success = vmTest.TryReopenLastRundown();

        // Assert: Should have reopened the newest file
        Assert.True(success);
        Assert.Equal(file2, vmTest.CurrentFilePath);
        Assert.Equal("Newer Project", vmTest.Rundown.SelectedRundown?.Name);
    }

    [Fact]
    public async Task ReopenPreviousProject_UsesLastOpenedFileInsteadOfOlderRecentFile()
    {
        string olderFile = Path.Combine(_tempDir, "Older.rundown");
        string latestFile = Path.Combine(_tempDir, "Latest.rundown");

        var olderVm = new MainWindowViewModel(autoReopenLastRundown: false);
        olderVm.Rundown.SelectedRundown!.Name = "Older Project";
        RundownProjectService.SaveProject(olderVm, olderFile);
        RundownProjectService.AddRecentFile(olderFile);

        var latestVm = new MainWindowViewModel(autoReopenLastRundown: false);
        latestVm.Rundown.SelectedRundown!.Name = "Latest Project";
        RundownProjectService.SaveProject(latestVm, latestFile);

        await latestVm.OpenFileDirectAsync(latestFile);
        Assert.Equal(Path.GetFullPath(latestFile), latestVm.Settings.General.LastOpenedRundownPath);
        Assert.Equal(Path.GetFullPath(latestFile), RundownProjectService.LoadRecentFiles()[0].FilePath);

        var reopenedVm = new MainWindowViewModel(autoReopenLastRundown: true);

        Assert.Equal(Path.GetFullPath(latestFile), reopenedVm.CurrentFilePath);
        Assert.Equal("Latest Project", reopenedVm.Rundown.SelectedRundown?.Name);
        Assert.False(reopenedVm.IsDirty);
        Assert.False(reopenedVm.HasUnsavedChanges);
    }
}
