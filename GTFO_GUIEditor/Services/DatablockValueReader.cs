using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace GTFO_GUIEditor.Services;

internal static class DatablockValueReader
{
    public static string GetString(JsonNode? node, string fallback = "")
    {
        if (node == null) return fallback;
        if (node is JsonValue value && value.TryGetValue<string>(out var text)) return text;
        return node.ToString();
    }

    public static string GetEnemyRole(JsonNode? node) => GetMapped(node, "Melee", new()
    {
        [0] = "Melee", [1] = "Ranged", [3] = "Lurker", [4] = "PureSneak",
        [5] = "Scout", [6] = "Patroller", [7] = "Hunter", [8] = "BirtherChild",
        [9] = "MiniBoss", [10] = "Boss"
    });

    public static string GetDifficulty(JsonNode? node) => GetMapped(node, "Easy", new()
    {
        [0] = "Easy", [1] = "Medium", [2] = "Hard", [3] = "MiniBoss",
        [4] = "Boss", [5] = "MegaBoss", [6] = "Biss", [7] = "Buss"
    });

    public static string GetGroupType(JsonNode? node) => GetMapped(node, "Hibernate", new()
    {
        [0] = "Hibernate", [1] = "PureSneak", [2] = "Detect", [3] = "PureDetect",
        [4] = "Patrol", [6] = "Hunter"
    });

    public static string GetSpawnPlacementType(JsonNode? node) => GetMapped(node, "Default", new()
    {
        [0] = "Default", [1] = "Align_0", [2] = "Align_1"
    });

    public static string GetDistribution(JsonNode? node) => GetMapped(node, "Rel_100", new()
    {
        [1] = "Force_One", [2] = "Rel_25", [3] = "Rel_50", [4] = "Rel_75",
        [5] = "Rel_100", [6] = "Rel_15", [7] = "Rel_10", [8] = "Rel_05"
    });

    public static string GetEnemyType(JsonNode? node) => GetMapped(node, "", new()
    {
        [0] = "Standard", [1] = "Standard", [2] = "Boss", [3] = "MiniBoss", [4] = "Weakling"
    });

    private static string GetMapped(JsonNode? node, string fallback, Dictionary<int, string> map)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var text)) return text;
        if (node is JsonValue number && number.TryGetValue<int>(out var numeric) && map.TryGetValue(numeric, out var mapped))
            return mapped;
        return GetString(node, fallback);
    }
}
