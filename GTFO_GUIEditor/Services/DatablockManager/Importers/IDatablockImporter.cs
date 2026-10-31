namespace GTFO_GUIEditor.Services;

/// <summary>
/// Strategy interface for modular datablock import handling.
/// </summary>
public interface IDatablockImporter
{
    /// <summary>
    /// Executes the import step using the shared import context.
    /// </summary>
    void Import(ImportContext context);
}
