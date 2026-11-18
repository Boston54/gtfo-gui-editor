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
                uint.TryParse(pid.ToString(), out uint id) &&
                (id == FogSettingsService.ElevatorFogPersistentId ||
                 string.Equals(b["name"]?.ToString(), "Fog_ELEVATOR", System.StringComparison.OrdinalIgnoreCase)));
            if (vanillaFog1 != null)
            {
                vanillaFog1["name"] = FogSettingsService.ElevatorFogName;
                vanillaFog1["persistentID"] = FogSettingsService.ElevatorFogPersistentId;
                fogBlocks.Insert(0, vanillaFog1);
            }
            else
            {
                fogBlocks.Insert(0, SerializeFogBlocks(new[]
                {
                    EditorDefaultsService.CreateDefaultFogBlock(
                        FogSettingsService.ElevatorFogPersistentId,
                        FogSettingsService.ElevatorFogName)
                })[0]);
            }
        }

        var elevatorFog = fogBlocks.First(b =>
            b["persistentID"]?.GetValue<uint>() == FogSettingsService.ElevatorFogPersistentId);
        elevatorFog["name"] = FogSettingsService.ElevatorFogName;

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

        var elevatorRainFog = fogBlocks.FirstOrDefault(b =>
            b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
            uint.TryParse(pid.ToString(), out uint id) && id == 96);
        if (elevatorRainFog == null)
        {
            var vanillaFog = ExportContext.LoadVanillaBlocksArray("FogSettingsDataBlock.json");
            elevatorRainFog = vanillaFog.FirstOrDefault(b =>
                b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                uint.TryParse(pid.ToString(), out uint id) && id == 96) ??
                SerializeFogBlocks(new[]
                {
                    EditorDefaultsService.CreateDefaultFogBlock(96, "FOG_ELEVATOR_RAIN")
                })[0];
            fogBlocks.Add(elevatorRainFog);
        }
        elevatorRainFog["persistentID"] = 96;
        elevatorRainFog["name"] = "FOG_ELEVATOR_RAIN";

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
                    ["z"] = b.DensityNoiseDirection?.Z ?? 0.0,
                    ["magnitude"] = GetDensityNoiseDirectionMagnitude(b),
                    ["sqrMagnitude"] = GetDensityNoiseDirectionSqrMagnitude(b)
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

    private static double GetDensityNoiseDirectionSqrMagnitude(FogSettingsBlock block)
    {
        double x = block.DensityNoiseDirection?.X ?? 0.0;
        double y = block.DensityNoiseDirection?.Y ?? 0.0;
        double z = block.DensityNoiseDirection?.Z ?? 0.0;
        return (x * x) + (y * y) + (z * z);
    }

    private static double GetDensityNoiseDirectionMagnitude(FogSettingsBlock block)
    {
        return System.Math.Sqrt(GetDensityNoiseDirectionSqrMagnitude(block));
    }
}
