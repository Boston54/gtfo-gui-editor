using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class LightSettingsDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        var lightBlocks = SerializeLightBlocks(context.MainVm.Lighting.Blocks);
        if (!lightBlocks.Any(b =>
                b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                uint.TryParse(pid.ToString(), out uint id) && id == 1))
        {
            var vanillaLight = ExportContext.LoadVanillaBlocksArray("LightSettingsDataBlock.json");
            var vanillaLight1 = vanillaLight.FirstOrDefault(b =>
                b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                uint.TryParse(pid.ToString(), out uint id) && id == 1);
            if (vanillaLight1 != null)
            {
                lightBlocks.Insert(0, vanillaLight1);
            }
        }

        context.LightSettingsBlocks.Clear();
        context.LightSettingsBlocks.AddRange(lightBlocks);

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_LightSettingsDataBlock_bin.json",
            context.LightSettingsBlocks);

        context.DatablocksExportedCount++;
    }

    private static System.Collections.Generic.List<JsonObject> SerializeLightBlocks(System.Collections.Generic.IEnumerable<LightSettingsBlock> blocks)
    {
        var list = new System.Collections.Generic.List<JsonObject>();
        foreach (var b in blocks)
        {
            if (b == null) continue;
            var cats = new JsonArray();
            foreach (var c in b.LightCategorySettings)
            {
                cats.Add(new JsonObject
                {
                    ["On"] = c.On,
                    ["Category"] = c.Category.ToString(),
                    ["Chance"] = c.Chance,
                    ["ChanceBroken"] = c.ChanceBroken,
                    ["Color"] = new JsonObject
                    {
                        ["a"] = c.Color.A,
                        ["r"] = c.Color.R,
                        ["g"] = c.Color.G,
                        ["b"] = c.Color.B
                    },
                    ["IntensityMul"] = c.IntensityMul,
                    ["Weight"] = c.Weight
                });
            }

            list.Add(new JsonObject
            {
                ["LightCategorySettings"] = cats,
                ["name"] = b.Name,
                ["internalEnabled"] = b.InternalEnabled,
                ["persistentID"] = b.PersistentId
            });
        }

        return list;
    }
}
