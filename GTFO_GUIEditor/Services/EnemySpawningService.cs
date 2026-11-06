using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class EnemySpawningService
{
    private static readonly Lazy<List<EnemySpawningOption>> _lazyOptions = new(LoadVanillaOptions);

    public static IReadOnlyList<EnemySpawningOption> AvailableOptions => _lazyOptions.Value;

    public static string GetDatablocksDirectory()
    {
        return AlarmService.GetDatablocksDirectory();
    }

    public static EnemySpawningOption? GetOption(string? groupType, string? difficulty)
    {
        if (string.IsNullOrWhiteSpace(groupType) || string.IsNullOrWhiteSpace(difficulty))
        {
            return AvailableOptions.FirstOrDefault();
        }

        return AvailableOptions.FirstOrDefault(o =>
            string.Equals(o.GroupType, groupType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(o.Difficulty, difficulty, StringComparison.OrdinalIgnoreCase));
    }

    private static List<EnemySpawningOption> LoadVanillaOptions()
    {
        return LoadAvailableOptions();
    }

    public static List<EnemySpawningOption> LoadAvailableOptions(string? datablocksDir = null)
    {
        datablocksDir ??= GetDatablocksDirectory();
        var resultList = new List<EnemySpawningOption>();

        string enemyDataPath = Path.Combine(datablocksDir, "EnemyDataBlock.json");
        string groupDataPath = Path.Combine(datablocksDir, "EnemyGroupDataBlock.json");
        string populationDataPath = Path.Combine(datablocksDir, "EnemyPopulationDataBlock.json");

        if (!File.Exists(enemyDataPath) || !File.Exists(groupDataPath) || !File.Exists(populationDataPath))
        {
            return resultList;
        }

        try
        {
            var enemiesJson = JsonNode.Parse(File.ReadAllText(enemyDataPath));
            var groupsJson = JsonNode.Parse(File.ReadAllText(groupDataPath));
            var populationJson = JsonNode.Parse(File.ReadAllText(populationDataPath));

            var enemies = new Dictionary<uint, string>();
            if (enemiesJson?["Blocks"] is JsonArray enemyBlocks)
            {
                foreach (var eb in enemyBlocks)
                {
                    if (eb is JsonObject ebObj)
                    {
                        uint pid = ebObj["persistentID"]?.GetValue<uint>() ?? 0;
                        string name = ebObj["name"]?.GetValue<string>() ?? string.Empty;
                        if (!string.IsNullOrEmpty(name))
                        {
                            name = name.Replace(" (Hibernate)", "", StringComparison.OrdinalIgnoreCase)
                                       .Replace("(Hibernate)", "", StringComparison.OrdinalIgnoreCase)
                                       .Trim();
                        }
                        if (pid > 0)
                        {
                            enemies[pid] = name;
                        }
                    }
                }
            }

            var roles = new Dictionary<(string Role, string Difficulty), List<(uint Enemy, double Weight)>>();
            if (populationJson?["Blocks"] is JsonArray popBlocks && popBlocks.Count > 0 && popBlocks[0] is JsonObject defaultPop)
            {
                if (defaultPop["RoleDatas"] is JsonArray roleDatas)
                {
                    foreach (var rd in roleDatas)
                    {
                        if (rd is JsonObject rdObj)
                        {
                            string role = rdObj["Role"]?.GetValue<string>() ?? string.Empty;
                            string difficulty = rdObj["Difficulty"]?.GetValue<string>() ?? string.Empty;
                            uint enemyId = rdObj["Enemy"]?.GetValue<uint>() ?? 0;
                            double weight = rdObj["Weight"]?.GetValue<double>() ?? 1.0;

                            var roleKey = (role, difficulty);
                            if (!roles.TryGetValue(roleKey, out var candidateList))
                            {
                                candidateList = new List<(uint Enemy, double Weight)>();
                                roles[roleKey] = candidateList;
                            }
                            candidateList.Add((enemyId, weight));
                        }
                    }
                }
            }

            // Key -> Dictionary of enemyName -> accumulated weight
            var mapping = new Dictionary<(string GroupType, string Difficulty), Dictionary<string, double>>();

            if (groupsJson?["Blocks"] is JsonArray groupBlocks)
            {
                foreach (var gb in groupBlocks)
                {
                    if (gb is not JsonObject groupObj) continue;

                    bool internalEnabled = groupObj["internalEnabled"]?.GetValue<bool>() ?? true;
                    if (!internalEnabled) continue;

                    string groupType = groupObj["Type"]?.GetValue<string>() ?? string.Empty;
                    if (string.Equals(groupType, "Hunter", StringComparison.OrdinalIgnoreCase)) continue;

                    string difficulty = groupObj["Difficulty"]?.GetValue<string>() ?? string.Empty;
                    double relativeWeight = groupObj["RelativeWeight"]?.GetValue<double>() ?? 1.0;

                    var key = (groupType, difficulty);

                    if (groupObj["Roles"] is JsonArray rolesArr)
                    {
                        foreach (var rNode in rolesArr)
                        {
                            if (rNode is not JsonObject rObj) continue;

                            string role = rObj["Role"]?.GetValue<string>() ?? string.Empty;
                            string distribution = rObj["Distribution"]?.GetValue<string>() ?? string.Empty;

                            if (!distribution.StartsWith("Rel_", StringComparison.OrdinalIgnoreCase)) continue;

                            string numPart = distribution.Substring(4);
                            if (!double.TryParse(numPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedVal))
                            {
                                continue;
                            }

                            double roleWeight = parsedVal / 100.0;

                            var roleKey = (role, difficulty);
                            if (!roles.TryGetValue(roleKey, out var candidates) || candidates.Count == 0)
                            {
                                continue;
                            }

                            double totalCandidateWeight = candidates.Sum(c => c.Weight);
                            if (totalCandidateWeight <= 0) continue;

                            if (!mapping.TryGetValue(key, out var enemyWeightMap))
                            {
                                enemyWeightMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                                mapping[key] = enemyWeightMap;
                            }

                            foreach (var candidate in candidates)
                            {
                                string enemyName = enemies.TryGetValue(candidate.Enemy, out var n)
                                    ? n
                                    : candidate.Enemy.ToString();

                                double calculatedWeight = relativeWeight * roleWeight * (candidate.Weight / totalCandidateWeight);

                                if (enemyWeightMap.TryGetValue(enemyName, out double currentWeight))
                                {
                                    enemyWeightMap[enemyName] = currentWeight + calculatedWeight;
                                }
                                else
                                {
                                    enemyWeightMap[enemyName] = calculatedWeight;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var kvp in mapping)
            {
                var key = kvp.Key;
                var enemyWeights = kvp.Value;
                double total = enemyWeights.Values.Sum();
                if (total <= 0) continue;

                var entries = enemyWeights
                    .OrderByDescending(x => x.Value)
                    .Select(x => new EnemyDistributionEntry
                    {
                        Name = x.Key,
                        Weight = Math.Round(x.Value, 4),
                        Proportion = Math.Round(x.Value / total, 4)
                    })
                    .ToList();

                resultList.Add(new EnemySpawningOption
                {
                    GroupType = key.GroupType,
                    Difficulty = key.Difficulty,
                    Enemies = entries
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnemySpawningService] Error loading available options: {ex.Message}");
        }

        return resultList;
    }
}
