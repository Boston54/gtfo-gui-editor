using System;
using System.Collections.Generic;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Single point of communication between .rundown files and the rest of the application.
/// Handles minimal, automatic saving and loading without manual per-field declarations.
/// </summary>
public interface IRundownLoader
{
    /// <summary>
    /// Exports the current application state from the view model into a minimal RundownProjectFile model.
    /// </summary>
    RundownProjectFile ExportProject(MainWindowViewModel vm);

    /// <summary>
    /// Saves the current view model state to the specified .rundown file path.
    /// </summary>
    void SaveProject(MainWindowViewModel vm, string filePath);

    /// <summary>
    /// Serializes and writes a RundownProjectFile model to disk.
    /// </summary>
    void SaveProject(RundownProjectFile project, string filePath);

    /// <summary>
    /// Reads and deserializes a .rundown project file from disk.
    /// </summary>
    RundownProjectFile LoadProject(string filePath);

    /// <summary>
    /// Applies the loaded rundown project data onto the main window view model and its child systems.
    /// </summary>
    void ApplyProject(MainWindowViewModel vm, RundownProjectFile project, string? filePath = null);

    /// <summary>
    /// Retrieves the list of recently opened files.
    /// </summary>
    List<RecentFileItem> LoadRecentFiles();

    /// <summary>
    /// Persists the list of recently opened files.
    /// </summary>
    void SaveRecentFiles(List<RecentFileItem> files);

    /// <summary>
    /// Adds or moves a file path to the top of the recent files list.
    /// </summary>
    void AddRecentFile(string filePath);
}
