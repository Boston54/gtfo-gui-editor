using System;
using System.Collections.Generic;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Facade providing backward compatibility for existing references by delegating to RundownLoader.
/// </summary>
public static class RundownProjectService
{
    public static string? CustomRecentFilesConfigPath
    {
        get => RundownLoader.CustomRecentFilesConfigPath;
        set => RundownLoader.CustomRecentFilesConfigPath = value;
    }

    public static RundownProjectFile ExportProject(MainWindowViewModel vm) => RundownLoader.Instance.ExportProject(vm);

    public static void SaveProject(MainWindowViewModel vm, string filePath) => RundownLoader.Instance.SaveProject(vm, filePath);

    public static void SaveProject(RundownProjectFile project, string filePath) => RundownLoader.Instance.SaveProject(project, filePath);

    public static RundownProjectFile LoadProject(string filePath) => RundownLoader.Instance.LoadProject(filePath);

    public static void ApplyProject(MainWindowViewModel vm, RundownProjectFile project, string? filePath = null) => RundownLoader.Instance.ApplyProject(vm, project, filePath);

    public static List<RecentFileItem> LoadRecentFiles() => RundownLoader.Instance.LoadRecentFiles();

    public static void SaveRecentFiles(List<RecentFileItem> files) => RundownLoader.Instance.SaveRecentFiles(files);

    public static void AddRecentFile(string filePath) => RundownLoader.Instance.AddRecentFile(filePath);
}
