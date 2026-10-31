using System.Collections.Generic;

namespace GTFO_GUIEditor.Models;

/// <summary>
/// Details of a level that failed to load during datablock and project import.
/// </summary>
public class LevelImportFailure
{
    public string LevelName { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string PublicName { get; set; } = string.Empty;
    public uint MainLayoutId { get; set; }
    public List<string> MissingConfigs { get; set; } = new();

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Prefix) && !string.IsNullOrWhiteSpace(PublicName)
            ? $"{Prefix} - {PublicName}"
            : (!string.IsNullOrWhiteSpace(PublicName)
                ? PublicName
                : (!string.IsNullOrWhiteSpace(Prefix) ? Prefix : $"Level_{MainLayoutId}"));

    public string MissingConfigsFormatted => string.Join(", ", MissingConfigs);

    public string FormattedDescription =>
        $"{DisplayName} (Layout ID: {MainLayoutId}): Missing config file for {MissingConfigsFormatted}";
}
