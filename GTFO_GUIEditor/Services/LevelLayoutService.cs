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
        var level = EditorDefaultsService.CreateDefaultLevel(persistentId, name, complexType, elevatorTile);
        return level;
    }

    public static LevelLayoutBlock CreateFreshLevel(uint persistentId, string name, uint complexResourceSetId, string elevatorTile)
    {
        return CreateFreshLevel(persistentId, name, ComplexType.Mining, elevatorTile, complexResourceSetId);
    }

    public static List<LevelLayoutBlock> GetInitialLevels(IEnumerable<ComplexResourceSetBlock>? complexResourceSets = null)
    {
        return new List<LevelLayoutBlock>
        {
            EditorDefaultsService.CreateDefaultLevel(1, "Level 1")
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
