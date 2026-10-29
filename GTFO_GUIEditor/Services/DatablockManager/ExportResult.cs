namespace GTFO_GUIEditor.Services;

/// <summary>
/// Represents the outcome of a datablock export operation.
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DatablocksExportedCount { get; set; }
    public int LevelGenFilesExportedCount { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
}
