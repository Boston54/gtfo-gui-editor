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
