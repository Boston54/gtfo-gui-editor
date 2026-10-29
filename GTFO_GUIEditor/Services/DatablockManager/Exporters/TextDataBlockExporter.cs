namespace GTFO_GUIEditor.Services;

public class TextDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_TextDataBlock_bin.json",
            context.TextBlocks);

        context.DatablocksExportedCount++;
    }
}
