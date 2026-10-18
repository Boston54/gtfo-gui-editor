using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class LevelLayoutService
{
    private static Dictionary<string, string>? _prefabToNameMap;

    public static LevelLayoutBlock CreateFreshLevel(uint persistentId, string name, ComplexType complexType, string elevatorTile, uint complexResourceSetId = 0)
    {
        return new LevelLayoutBlock
        {
            PersistentId = persistentId,
            Name = name,
            InternalEnabled = true,
            ComplexType = complexType,
            ComplexResourceSet = complexResourceSetId,
            ElevatorTile = elevatorTile ?? string.Empty
        };
    }

    public static LevelLayoutBlock CreateFreshLevel(uint persistentId, string name, uint complexResourceSetId, string elevatorTile)
    {
        return CreateFreshLevel(persistentId, name, ComplexType.Mining, elevatorTile, complexResourceSetId);
    }

    public static List<LevelLayoutBlock> GetInitialLevels(IEnumerable<ComplexResourceSetBlock>? complexResourceSets = null)
    {
        var sets = (complexResourceSets ?? ComplexResourceSetService.LoadVanillaBlocks()).ToList();
        var firstSet = sets.FirstOrDefault();

        uint defaultSetId = firstSet?.PersistentId ?? 1;
        ComplexType defaultComplexType = firstSet?.ComplexType ?? ComplexType.Mining;
        var elevatorOptions = GeomorphService.GetElevatorOptionsForComplex(defaultComplexType);
        string defaultElevator = elevatorOptions.FirstOrDefault()?.Prefab
            ?? firstSet?.ElevatorShafts_1x1.FirstOrDefault()?.Prefab
            ?? "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab";

        return new List<LevelLayoutBlock>
        {
            CreateFreshLevel(1, "Level 1", defaultComplexType, defaultElevator, defaultSetId)
        };
    }

    public static string GetFriendlyElevatorName(string prefab)
    {
        if (string.IsNullOrWhiteSpace(prefab)) return string.Empty;

        if (_prefabToNameMap == null)
        {
            _prefabToNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var mappings = GeomorphService.LoadMappings();
            foreach (var kvp in mappings)
            {
                if (!_prefabToNameMap.ContainsKey(kvp.Value))
                {
                    _prefabToNameMap[kvp.Value] = kvp.Key;
                }
            }
        }

        if (_prefabToNameMap.TryGetValue(prefab, out string? mappedName))
        {
            return mappedName;
        }

        string fileName = Path.GetFileNameWithoutExtension(prefab);
        return string.IsNullOrWhiteSpace(fileName) ? prefab : fileName;
    }
}
