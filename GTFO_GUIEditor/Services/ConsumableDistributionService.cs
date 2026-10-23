using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class ConsumableDistributionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string GetDatablocksDirectory()
    {
        return LightSettingsService.GetDatablocksDirectory();
    }

    public static string? FindDataBlockFile()
    {
        string dir = GetDatablocksDirectory();
        string directPath = Path.Combine(dir, "ConsumableDistributionDataBlock.json");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        string directPath2 = Path.Combine(dir, "ConsumableDistributionDatablock.json");
        if (File.Exists(directPath2))
        {
            return directPath2;
        }

        string altPath = Path.Combine(dir, "GameData_ConsumableDistributionDataBlock_bin.json");
        if (File.Exists(altPath))
        {
            return altPath;
        }

        return null;
    }

    public static ConsumableDistributionDataBlockFile LoadDataBlockFile(string? filePath = null)
    {
        filePath ??= FindDataBlockFile();

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new ConsumableDistributionDataBlockFile();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ConsumableDistributionDataBlockFile>(json, JsonOptions) ?? new ConsumableDistributionDataBlockFile();
        }
        catch
        {
            return new ConsumableDistributionDataBlockFile();
        }
    }

    public static List<ConsumableDistributionBlock> LoadVanillaBlocks(string? filePath = null)
    {
        var file = LoadDataBlockFile(filePath);
        return file.Blocks;
    }

    public static ConsumableDistributionBlock CreateFreshBlock(uint id, string name)
    {
        return new ConsumableDistributionBlock
        {
            PersistentId = id,
            Name = name,
            InternalEnabled = true,
            SpawnsPerZone = 5,
            ChanceToSpawnInResourceContainer = 0.8,
            SpawnData = new ObservableCollection<ConsumableDistributionItem>
            {
                new ConsumableDistributionItem(Consumables.Glowstick, 10.0),
                new ConsumableDistributionItem(Consumables.Flashlight, 5.0),
                new ConsumableDistributionItem(Consumables.LockMelter, 2.0),
                new ConsumableDistributionItem(Consumables.CFoamGrenade, 2.0),
                new ConsumableDistributionItem(Consumables.FogRepeller, 2.0)
            }
        };
    }

    public static ConsumableDistributionBlock CreateBlockFromTemplate(ConsumableDistributionBlock template, uint newId, string newName)
    {
        var clone = template.Clone();
        clone.PersistentId = newId;
        clone.Name = newName;
        return clone;
    }
}
