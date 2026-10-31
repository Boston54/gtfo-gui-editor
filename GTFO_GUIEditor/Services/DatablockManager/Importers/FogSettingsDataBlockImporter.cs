using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class FogSettingsDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        var blocks = new ObservableCollection<FogSettingsBlock>();

        foreach (var kvp in context.FogSettingsBlocks)
        {
            var obj = kvp.Value;
            if (obj == null) continue;

            uint persistentId = kvp.Key;
            string name = obj["name"]?.ToString() ?? $"Fog_{persistentId}";
            bool internalEnabled = obj["internalEnabled"]?.GetValue<bool>() ?? true;
            bool enabled = obj["Enabled"]?.GetValue<bool>() ?? true;

            var fogColor = new LightColor();
            if (obj["FogColor"] is JsonObject colObj)
            {
                double a = colObj["a"]?.GetValue<double>() ?? 0.0;
                double r = colObj["r"]?.GetValue<double>() ?? 0.0;
                double g = colObj["g"]?.GetValue<double>() ?? 0.0;
                double b = colObj["b"]?.GetValue<double>() ?? 0.0;
                fogColor = new LightColor { A = a, R = r, G = g, B = b };
            }

            var dir = new Vector3Data(0.0, 0.0, 0.0);
            if (obj["DensityNoiseDirection"] is JsonObject dirObj)
            {
                double x = dirObj["x"]?.GetValue<double>() ?? 0.0;
                double y = dirObj["y"]?.GetValue<double>() ?? 0.0;
                double z = dirObj["z"]?.GetValue<double>() ?? 0.0;
                dir = new Vector3Data(x, y, z);
            }

            double fogDensity = obj["FogDensity"]?.GetValue<double>() ?? 0.0;
            double fogAmbience = obj["FogAmbience"]?.GetValue<double>() ?? 0.0;
            double densityNoiseSpeed = obj["DensityNoiseSpeed"]?.GetValue<double>() ?? 0.0;
            double densityNoiseScale = obj["DensityNoiseScale"]?.GetValue<double>() ?? 0.0;
            double densityHeightAltitude = obj["DensityHeightAltitude"]?.GetValue<double>() ?? 0.0;
            double densityHeightRange = obj["DensityHeightRange"]?.GetValue<double>() ?? 0.0;
            double densityHeightMaxBoost = obj["DensityHeightMaxBoost"]?.GetValue<double>() ?? 0.0;
            double infection = obj["Infection"]?.GetValue<double>() ?? 0.0;

            var fogBlock = new FogSettingsBlock
            {
                PersistentId = persistentId,
                Name = name,
                InternalEnabled = internalEnabled,
                Enabled = enabled,
                FogColor = fogColor,
                FogDensity = fogDensity,
                FogAmbience = fogAmbience,
                DensityNoiseDirection = dir,
                DensityNoiseSpeed = densityNoiseSpeed,
                DensityNoiseScale = densityNoiseScale,
                DensityHeightAltitude = densityHeightAltitude,
                DensityHeightRange = densityHeightRange,
                DensityHeightMaxBoost = densityHeightMaxBoost,
                Infection = infection
            };

            blocks.Add(fogBlock);
        }

        context.MainVm.FogSettings.Blocks.Clear();
        foreach (var b in blocks)
        {
            context.MainVm.FogSettings.Blocks.Add(b);
        }
        if (context.MainVm.FogSettings.Blocks.Count > 0)
        {
            context.MainVm.FogSettings.SelectedBlock = context.MainVm.FogSettings.Blocks[0];
        }
    }
}
