using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class LevelRefsService
{
    public const string FileName = "LevelRefs.json";

    public static Dictionary<uint, LevelRefData> Load(string rootPath)
    {
        var result = new Dictionary<uint, LevelRefData>();
        string path = Path.Combine(rootPath, FileName);
        if (!File.Exists(path)) return result;
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(path));
            JsonArray? entries = root as JsonArray;
            if (root is JsonObject obj) entries = obj["Levels"] as JsonArray ?? obj["Blocks"] as JsonArray;
            if (entries == null) return result;
            foreach (var node in entries)
            {
                var data = node?.Deserialize<LevelRefData>(ExportContext.JsonOptions);
                if (data != null && data.PersistentId > 0) result[data.PersistentId] = data;
            }
        }
        catch { }
        return result;
    }

    public static void Write(string rootPath, IEnumerable<LevelLayoutBlock> levels)
    {
        var entries = new JsonArray();
        foreach (var level in levels.Where(l => l != null && l.PersistentId > 0).OrderBy(l => l.PersistentId))
        {
            var reference = level.LevelRef?.Clone() ?? new LevelRefData();
            reference.PersistentId = level.PersistentId;
            entries.Add(JsonSerializer.SerializeToNode(reference, ExportContext.JsonOptions));
        }
        File.WriteAllText(Path.Combine(rootPath, FileName),
            new JsonObject { ["Levels"] = entries }.ToJsonString(ExportContext.JsonOptions));
    }

    public static void ApplyToLevel(LevelLayoutBlock level, LevelRefData? data)
    {
        if (data == null) return;
        level.LevelRef = data.Clone();
        level.LevelRef.PersistentId = level.PersistentId;
        if (data.FogSetting > 0) level.FogSettings = data.FogSetting;
    }

    public static void ImportLegacyRundownValues(
        LevelLayoutBlock level,
        ExpeditionBlock expedition,
        bool importObjectiveData = true)
    {
        var reference = level.LevelRef ?? new LevelRefData { PersistentId = level.PersistentId };
        if (string.IsNullOrWhiteSpace(reference.Description)) reference.Description = expedition.Descriptive?.ExpeditionDescription ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reference.WardenIntel)) reference.WardenIntel = expedition.Descriptive?.RoleplayedWardenIntel ?? string.Empty;
        if (reference.PlayerStartingState == null || IsDefault(reference.PlayerStartingState)) reference.PlayerStartingState = expedition.SpecialOverrideData?.Clone() ?? new SpecialOverrideData();
        if (reference.FogSetting == 0) reference.FogSetting = expedition.Expedition?.FogSettings ?? level.FogSettings;
        if (reference.DefaultLightSetting == 0) reference.DefaultLightSetting = expedition.Expedition?.LightSettings ?? 0;
        if (reference.MainObjectiveId == 0) reference.MainObjectiveId = level.PersistentId;
        if (reference.SecondaryObjectiveId == 0) reference.SecondaryObjectiveId = expedition.SecondaryLayerData?.ObjectiveData?.DataBlockId ?? 0;
        if (reference.OverloadObjectiveId == 0) reference.OverloadObjectiveId = expedition.ThirdLayerData?.ObjectiveData?.DataBlockId ?? 0;
        if (importObjectiveData)
        {
            ImportObjectiveData(reference.MainObjective, expedition.MainLayerData?.ObjectiveData);
            ImportObjectiveData(reference.SecondaryObjective, expedition.SecondaryLayerData?.ObjectiveData);
            ImportObjectiveData(reference.OverloadObjective, expedition.ThirdLayerData?.ObjectiveData);
        }
        level.LevelRef = reference;
    }

    private static void ImportObjectiveData(LevelObjectiveRefData target, ExpeditionObjectiveData? source)
    {
        if (source == null) return;

        if (Enum.TryParse<WinCondition>(source.WinCondition, true, out var winCondition))
        {
            target.WinCondition = winCondition;
        }

        if (source.ZonePlacementDatas is { } placements &&
            placements.ValueKind == JsonValueKind.Array)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<System.Collections.ObjectModel.ObservableCollection<System.Collections.ObjectModel.ObservableCollection<ZonePlacementData>>>(
                    placements.GetRawText(), ExportContext.JsonOptions);
                if (parsed != null)
                {
                    target.ZonePlacementDatas = parsed;
                }
            }
            catch
            {
                // Preserve the default empty placement list when legacy data is malformed.
            }
        }
    }

    private static bool IsDefault(SpecialOverrideData value) =>
        value.InfectionLevelAtExpeditionStart == 0 && value.HealthLevelAtExpeditionStart == 1 &&
        value.StandardAmmoAtExpeditionStart == 1 && value.SpecialAmmoAtExpeditionStart == 1 &&
        value.ToolAmmoAtExpeditionStart == 1;
}
