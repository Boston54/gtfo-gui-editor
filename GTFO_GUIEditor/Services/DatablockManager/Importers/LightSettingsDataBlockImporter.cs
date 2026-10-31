using System;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class LightSettingsDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        var blocks = new ObservableCollection<LightSettingsBlock>();

        foreach (var kvp in context.LightSettingsBlocks)
        {
            var obj = kvp.Value;
            if (obj == null) continue;

            uint persistentId = kvp.Key;
            string name = obj["name"]?.ToString() ?? $"Light_{persistentId}";
            bool internalEnabled = obj["internalEnabled"]?.GetValue<bool>() ?? true;

            var lightBlock = new LightSettingsBlock
            {
                PersistentId = persistentId,
                Name = name,
                InternalEnabled = internalEnabled,
                LightCategorySettings = new ObservableCollection<LightCategorySettingItem>()
            };

            if (obj["LightCategorySettings"] is JsonArray catsArray)
            {
                foreach (var catNode in catsArray)
                {
                    if (catNode is not JsonObject catObj) continue;

                    string catStr = catObj["Category"]?.ToString() ?? "General";
                    if (!Enum.TryParse<LightCategory>(catStr, true, out var category))
                    {
                        if (int.TryParse(catStr, out int catInt) && Enum.IsDefined(typeof(LightCategory), catInt))
                        {
                            category = (LightCategory)catInt;
                        }
                        else
                        {
                            category = LightCategory.General;
                        }
                    }

                    bool on = catObj["On"]?.GetValue<bool>() ?? true;
                    double chance = catObj["Chance"]?.GetValue<double>() ?? 1.0;
                    double chanceBroken = catObj["ChanceBroken"]?.GetValue<double>() ?? 0.0;
                    double intensityMul = catObj["IntensityMul"]?.GetValue<double>() ?? 1.0;
                    double weight = catObj["Weight"]?.GetValue<double>() ?? 1.0;

                    var color = new LightColor { A = 1.0, R = 1.0, G = 1.0, B = 1.0 };
                    if (catObj["Color"] is JsonObject colObj)
                    {
                        double a = colObj["a"]?.GetValue<double>() ?? 1.0;
                        double r = colObj["r"]?.GetValue<double>() ?? 1.0;
                        double g = colObj["g"]?.GetValue<double>() ?? 1.0;
                        double b = colObj["b"]?.GetValue<double>() ?? 1.0;
                        color = new LightColor { A = a, R = r, G = g, B = b };
                    }

                    lightBlock.LightCategorySettings.Add(new LightCategorySettingItem
                    {
                        Category = category,
                        On = on,
                        Chance = chance,
                        ChanceBroken = chanceBroken,
                        IntensityMul = intensityMul,
                        Weight = weight,
                        Color = color
                    });
                }
            }

            blocks.Add(lightBlock);
        }

        context.MainVm.Lighting.Blocks.Clear();
        foreach (var b in blocks)
        {
            context.MainVm.Lighting.Blocks.Add(b);
        }
        if (context.MainVm.Lighting.Blocks.Count > 0)
        {
            context.MainVm.Lighting.SelectedBlock = context.MainVm.Lighting.Blocks[0];
        }
    }
}
