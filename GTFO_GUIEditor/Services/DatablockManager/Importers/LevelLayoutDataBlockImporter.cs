using System.Collections.Generic;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class LevelLayoutDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        var path = context.FindFile(
            "LevelLayoutDataBlock.json",
            "GameData_LevelLayoutDataBlock_bin.json");
        if (!context.TryDeserializeFile<LevelLayoutDataBlockFile>(path, out var file) || file?.Blocks == null)
        {
            return;
        }

        var target = DatablockManager.Instance.Get<LevelLayoutBlock>();
        target.Clear();
        foreach (var block in file.Blocks)
        {
            target.Add(block);
        }
    }

    private sealed class LevelLayoutDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<LevelLayoutBlock>? Blocks { get; set; }
    }
}
