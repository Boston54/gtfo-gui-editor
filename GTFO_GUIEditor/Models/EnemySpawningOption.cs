using System;
using System.Collections.Generic;
using System.Linq;

namespace GTFO_GUIEditor.Models;

public class EnemyDistributionEntry
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double Proportion { get; set; }
}

public class EnemySpawningOption
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<EnemyDistributionEntry> Enemies { get; set; } = new();

    public string DisplayName => string.IsNullOrWhiteSpace(GroupName)
        ? $"{GroupType} {Difficulty}".Trim()
        : GroupName;

    public string DistributionText => string.Join(", ", Enemies.Select(e => $"{e.Name}: {Math.Round(e.Proportion * 100, 1)}%"));

    public override string ToString() => DisplayName;
}
