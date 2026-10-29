using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class FogSettingsDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        var fogBlocks = SerializeFogBlocks(context.MainVm.FogSettings.Blocks);
        if (!fogBlocks.Any(b =>
                b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                uint.TryParse(pid.ToString(), out uint id) && id == 1))
        {
            var vanillaFog = ExportContext.LoadVanillaBlocksArray("FogSettingsDataBlock.json");
            var vanillaFog1 = vanillaFog.FirstOrDefault(b =>
                b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                uint.TryParse(pid.ToString(), out uint id) && id == 1);
            if (vanillaFog1 != null)
            {
                fogBlocks.Insert(0, vanillaFog1);
            }
        }

        if (!fogBlocks.Any(b =>
                b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                uint.TryParse(pid.ToString(), out uint id) && id == 21))
        {
            var vanillaFog = ExportContext.LoadVanillaBlocksArray("FogSettingsDataBlock.json");
            var vanillaFog21 = vanillaFog.FirstOrDefault(b =>
                b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                uint.TryParse(pid.ToString(), out uint id) && id == 21);
            if (vanillaFog21 != null)
            {
                fogBlocks.Add(vanillaFog21);
            }
        }

        context.FogSettingsBlocks.Clear();
        context.FogSettingsBlocks.AddRange(fogBlocks);

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_FogSettingsDataBlock_bin.json",
            context.FogSettingsBlocks);

        context.DatablocksExportedCount++;
    }

    private static List<JsonObject> SerializeFogBlocks(IEnumerable<FogSettingsBlock> blocks)
    {
        var list = new List<JsonObject>();
        foreach (var b in blocks)
        {
            if (b == null) continue;
            list.Add(new JsonObject
            {
                ["Enabled"] = b.Enabled,
                ["FogColor"] = new JsonObject
                {
                    ["a"] = b.FogColor?.A ?? 0.0,
                    ["r"] = b.FogColor?.R ?? 0.0,
                    ["g"] = b.FogColor?.G ?? 0.0,
                    ["b"] = b.FogColor?.B ?? 0.0
                },
                ["FogDensity"] = b.FogDensity,
                ["FogAmbience"] = b.FogAmbience,
                ["DensityNoiseDirection"] = new JsonObject
                {
                    ["x"] = b.DensityNoiseDirection?.X ?? 0.0,
                    ["y"] = b.DensityNoiseDirection?.Y ?? 0.0,
                    ["z"] = b.DensityNoiseDirection?.Z ?? 0.0
                },
                ["DensityNoiseSpeed"] = b.DensityNoiseSpeed,
                ["DensityNoiseScale"] = b.DensityNoiseScale,
                ["DensityHeightAltitude"] = b.DensityHeightAltitude,
                ["DensityHeightRange"] = b.DensityHeightRange,
                ["DensityHeightMaxBoost"] = b.DensityHeightMaxBoost,
                ["Infection"] = b.Infection,
                ["name"] = b.Name,
                ["internalEnabled"] = b.InternalEnabled,
                ["persistentID"] = b.PersistentId
            });
        }

        return list;
    }
}
