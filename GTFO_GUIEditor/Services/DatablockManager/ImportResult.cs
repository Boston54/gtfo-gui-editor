using System.Collections.Generic;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Encapsulates the results of a datablock & config import operation.
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string InputDirectory { get; set; } = string.Empty;
    public int DatablocksImportedCount { get; set; }
    public int LevelsImportedCount { get; set; }
    public List<string> SkippedLevels { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<LevelImportFailure> FailedLevels { get; set; } = new();
}
