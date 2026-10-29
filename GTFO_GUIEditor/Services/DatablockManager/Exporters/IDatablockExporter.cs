namespace GTFO_GUIEditor.Services;

/// <summary>
/// Defines the contract for an individual datablock exporter module.
/// </summary>
public interface IDatablockExporter
{
    /// <summary>
    /// Processes and exports the datablock data using the provided export context.
    /// </summary>
    void Export(ExportContext context);
}
