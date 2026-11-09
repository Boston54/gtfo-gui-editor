using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class EnemySpawningTests
{
    [Fact]
    public void EnemySpawningService_RefreshOptions_WeightsAndFiltersCandidates()
    {
        var group = new EnemyGroupBlock
        {
            Type = "Hibernate",
            Difficulty = "Easy",
            RelativeWeight = 2
        };
        group.Roles.Add(new EnemyGroupRole { Role = "Striker", Distribution = "Rel_50" });
        group.Roles.Add(new EnemyGroupRole { Role = "Shooter", Distribution = "Force_One" });
        group.Roles.Add(new EnemyGroupRole { Role = "Ignored", Distribution = "Invalid" });

        var disabledGroup = new EnemyGroupBlock
        {
            Type = "Disabled",
            Difficulty = "Easy",
            InternalEnabled = false
        };
        disabledGroup.Roles.Add(new EnemyGroupRole { Role = "Striker", Distribution = "Rel_100" });

        var roles = new[]
        {
            new EnemyRoleData { Role = "striker", Difficulty = "easy", Enemy = 1, Weight = 1 },
            new EnemyRoleData { Role = "Striker", Difficulty = "Easy", Enemy = 2, Weight = 3 },
            new EnemyRoleData { Role = "Shooter", Difficulty = "Easy", Enemy = 2, Weight = 2 },
            new EnemyRoleData { Role = "Striker", Difficulty = "Hard", Enemy = 3, Weight = 1 },
            new EnemyRoleData { Role = "Ignored", Difficulty = "Easy", Enemy = 4, Weight = 1 }
        };
        var enemies = new[]
        {
            new EnemyEntry { PersistentId = 1, Name = "Striker (Hibernate)" },
            new EnemyEntry { PersistentId = 2, Name = "Shooter" },
            new EnemyEntry { PersistentId = 3, Name = "Hard Enemy" },
            new EnemyEntry { PersistentId = 4, Name = "Ignored Enemy" },
            new EnemyEntry { PersistentId = 0, Name = "None" }
        };

        try
        {
            EnemySpawningService.RefreshOptions(new[] { group, disabledGroup }, roles, enemies);

            var option = Assert.Single(EnemySpawningService.AvailableOptions);
            Assert.Equal("Hibernate", option.GroupType);
            Assert.Equal("Easy", option.Difficulty);
            var striker = Assert.Single(option.Enemies.Where(e => e.Name == "Striker"));
            var shooter = Assert.Single(option.Enemies.Where(e => e.Name == "Shooter"));
            Assert.Equal(0.25, striker.Weight, 6);
            Assert.Equal(2.75, shooter.Weight, 6);
            Assert.Equal(1.0 / 12, striker.Proportion, 6);
            Assert.Equal(11.0 / 12, shooter.Proportion, 6);
        }
        finally
        {
            EnemySpawningService.RefreshOptions(Array.Empty<EnemyGroupBlock>(), Array.Empty<EnemyRoleData>());
        }
    }

    [Fact]
    public void EnemySpawningService_GetOption_MatchesCaseInsensitivelyAndHandlesMissingValues()
    {
        var group = new EnemyGroupBlock { Type = "Hibernate", Difficulty = "Easy" };
        group.Roles.Add(new EnemyGroupRole { Role = "Striker", Distribution = "Rel_100" });
        var role = new EnemyRoleData { Role = "Striker", Difficulty = "Easy", Enemy = 1, Weight = 1 };

        try
        {
            EnemySpawningService.RefreshOptions(new[] { group }, new[] { role });

            Assert.Same(EnemySpawningService.AvailableOptions[0], EnemySpawningService.GetOption("hibernate", "easy"));
            Assert.Null(EnemySpawningService.GetOption("Missing", "Easy"));
            Assert.Same(EnemySpawningService.AvailableOptions[0], EnemySpawningService.GetOption(null, "Easy"));
        }
        finally
        {
            EnemySpawningService.RefreshOptions(Array.Empty<EnemyGroupBlock>(), Array.Empty<EnemyRoleData>());
        }
    }

    [Fact]
    public void EnemySpawningService_LoadsOptionsCorrectly()
    {
        var options = EnemySpawningService.AvailableOptions;
        Assert.NotNull(options);
        Assert.NotEmpty(options);

        // Verify Hunter groups are excluded
        Assert.DoesNotContain(options, o => string.Equals(o.GroupType, "Hunter", StringComparison.OrdinalIgnoreCase));

        // Verify Hibernate Easy option exists
        var hibernateEasy = options.FirstOrDefault(o =>
            string.Equals(o.GroupType, "Hibernate", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(o.Difficulty, "Easy", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(hibernateEasy);
        Assert.NotEmpty(hibernateEasy.Enemies);
        Assert.False(string.IsNullOrEmpty(hibernateEasy.DisplayName));
        Assert.False(string.IsNullOrEmpty(hibernateEasy.DistributionText));
        Assert.DoesNotContain("(Hibernate)", hibernateEasy.DisplayName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("(Hibernate)", hibernateEasy.DistributionText, StringComparison.OrdinalIgnoreCase);

        // All enemies across all options should not have (Hibernate)
        foreach (var option in options)
        {
            foreach (var enemy in option.Enemies)
            {
                Assert.DoesNotContain("(Hibernate)", enemy.Name, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Total proportion should sum to approximately 1.0 (or 100%)
        double sumProportion = hibernateEasy.Enemies.Sum(e => e.Proportion);
        Assert.InRange(sumProportion, 0.99, 1.01);
    }

    [Fact]
    public void EnemySpawningInZoneItem_SelectedOption_SyncsWithGroupTypeAndDifficulty()
    {
        var item = new EnemySpawningInZoneItem
        {
            GroupType = "Hibernate",
            Difficulty = "Easy",
            Distribution = EnemyZoneDistribution.Rel_Value,
            DistributionValue = 0.4
        };

        Assert.NotNull(item.SelectedOption);
        Assert.Equal("Hibernate", item.SelectedOption.GroupType);
        Assert.Equal("Easy", item.SelectedOption.Difficulty);
        Assert.True(item.IsRelValue);
        Assert.True(item.HasDistributionText);

        // Change distribution to Force_One
        item.Distribution = EnemyZoneDistribution.Force_One;
        Assert.False(item.IsRelValue);

        // Change SelectedOption to another option
        var targetOption = EnemySpawningService.AvailableOptions.FirstOrDefault(o =>
            o.GroupType != "Hibernate" || o.Difficulty != "Easy");

        if (targetOption != null)
        {
            item.SelectedOption = targetOption;
            Assert.Equal(targetOption.GroupType, item.GroupType);
            Assert.Equal(targetOption.Difficulty, item.Difficulty);
        }
    }

    [Fact]
    public void EnemySpawningInZoneItem_Clone_CreatesIndependentCopy()
    {
        var original = new EnemySpawningInZoneItem
        {
            GroupType = "PureSneak",
            Difficulty = "Medium",
            Distribution = EnemyZoneDistribution.Rel_Value,
            DistributionValue = 0.75
        };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(original.GroupType, clone.GroupType);
        Assert.Equal(original.Difficulty, clone.Difficulty);
        Assert.Equal(original.Distribution, clone.Distribution);
        Assert.Equal(original.DistributionValue, clone.DistributionValue);

        // Modifying clone does not affect original
        clone.DistributionValue = 0.25;
        Assert.Equal(0.75, original.DistributionValue);
    }

    [Fact]
    public void GridState_ExportAndImport_PreservesEnemySpawningInZone()
    {
        var grid = new LevelLayoutGridState();
        var zs = grid.GetOrCreateZoneSettings(1);

        zs.EnemySpawningInZone.Add(new EnemySpawningInZoneItem
        {
            GroupType = "Hibernate",
            Difficulty = "Easy",
            Distribution = EnemyZoneDistribution.Rel_Value,
            DistributionValue = 0.4
        });

        zs.EnemySpawningInZone.Add(new EnemySpawningInZoneItem
        {
            GroupType = "PureDetect",
            Difficulty = "Hard",
            Distribution = EnemyZoneDistribution.Force_One,
            DistributionValue = 0.0
        });

        var stateData = grid.ExportStateData();
        Assert.NotNull(stateData.ZoneSettings);

        var zoneData = stateData.ZoneSettings.FirstOrDefault(z => z.ZoneIndex == 1);
        Assert.NotNull(zoneData);
        Assert.Equal(2, zoneData.EnemySpawningInZone.Count);

        var newGrid = new LevelLayoutGridState();
        newGrid.ImportStateData(stateData);

        var importedZs = newGrid.GetOrCreateZoneSettings(1);
        Assert.Equal(2, importedZs.EnemySpawningInZone.Count);

        Assert.Equal("Hibernate", importedZs.EnemySpawningInZone[0].GroupType);
        Assert.Equal("Easy", importedZs.EnemySpawningInZone[0].Difficulty);
        Assert.Equal(EnemyZoneDistribution.Rel_Value, importedZs.EnemySpawningInZone[0].Distribution);
        Assert.Equal(0.4, importedZs.EnemySpawningInZone[0].DistributionValue);

        Assert.Equal("PureDetect", importedZs.EnemySpawningInZone[1].GroupType);
        Assert.Equal("Hard", importedZs.EnemySpawningInZone[1].Difficulty);
        Assert.Equal(EnemyZoneDistribution.Force_One, importedZs.EnemySpawningInZone[1].Distribution);
    }

    [Fact]
    public void LevelLayoutDataBlock_ExportAndImport_EnemySpawningInZoneRoundtrip()
    {
        var mainVm = new MainWindowViewModel();
        var level = new LevelLayoutBlock { Name = "Test Level", PersistentId = 100 };
        mainVm.LevelLayout.Levels.Add(level);

        var zs = level.GridState.GetOrCreateZoneSettings(SectorType.Main, 0);
        zs.EnemySpawningInZone.Add(new EnemySpawningInZoneItem
        {
            GroupType = "Hibernate",
            Difficulty = "Easy",
            Distribution = EnemyZoneDistribution.Rel_Value,
            DistributionValue = 0.4
        });

        zs.EnemySpawningInZone.Add(new EnemySpawningInZoneItem
        {
            GroupType = "PureSneak",
            Difficulty = "Hard",
            Distribution = EnemyZoneDistribution.Force_One,
            DistributionValue = 0.0
        });

        string tempDir = Path.Combine(Path.GetTempPath(), "GTFO_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var exportCtx = new ExportContext(mainVm, tempDir);
            var exporter = new LevelLayoutDataBlockExporter();
            exporter.Export(exportCtx);

            string layoutJsonPath = Path.Combine(exportCtx.ExportRootPath, "GameData_LevelLayoutDataBlock_bin.json");
            Assert.True(File.Exists(layoutJsonPath));

            string jsonContent = File.ReadAllText(layoutJsonPath);
            var parsed = JsonNode.Parse(jsonContent);
            Assert.NotNull(parsed);

            var blocks = parsed["Blocks"] as JsonArray;
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);

            var mainBlock = blocks.OfType<JsonObject>().FirstOrDefault(b => b["persistentID"]?.GetValue<uint>() == 100);
            Assert.NotNull(mainBlock);
            var zones = mainBlock["Zones"] as JsonArray;
            Assert.NotNull(zones);
            Assert.NotEmpty(zones);

            var zone0 = zones.OfType<JsonObject>().FirstOrDefault(z => z["LocalIndex"]?.GetValue<string>() == "Zone_0");
            Assert.NotNull(zone0);

            var enemySpawningInZone = zone0["EnemySpawningInZone"] as JsonArray;
            Assert.NotNull(enemySpawningInZone);
            Assert.Equal(2, enemySpawningInZone.Count);

            var firstItem = enemySpawningInZone[0] as JsonObject;
            Assert.NotNull(firstItem);
            Assert.Equal("Hibernate", firstItem["GroupType"]?.GetValue<string>());
            Assert.Equal("Easy", firstItem["Difficulty"]?.GetValue<string>());
            Assert.Equal("Rel_Value", firstItem["Distribution"]?.GetValue<string>());
            Assert.Equal(0.4, firstItem["DistributionValue"]?.GetValue<double>());

            // Test Import
            var importMainVm = new MainWindowViewModel();
            var importCtx = new ImportContext(importMainVm, tempDir);
            var importer = new LevelLayoutDataBlockImporter();
            importer.Import(importCtx);

            var importedLevel = importMainVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == 100);
            Assert.NotNull(importedLevel);

            var importedZs = importedLevel.GridState.GetOrCreateZoneSettings(SectorType.Main, 0);
            Assert.NotNull(importedZs);
            Assert.Equal(2, importedZs.EnemySpawningInZone.Count);

            Assert.Equal("Hibernate", importedZs.EnemySpawningInZone[0].GroupType);
            Assert.Equal("Easy", importedZs.EnemySpawningInZone[0].Difficulty);
            Assert.Equal(EnemyZoneDistribution.Rel_Value, importedZs.EnemySpawningInZone[0].Distribution);
            Assert.Equal(0.4, importedZs.EnemySpawningInZone[0].DistributionValue);

            Assert.Equal("PureSneak", importedZs.EnemySpawningInZone[1].GroupType);
            Assert.Equal("Hard", importedZs.EnemySpawningInZone[1].Difficulty);
            Assert.Equal(EnemyZoneDistribution.Force_One, importedZs.EnemySpawningInZone[1].Distribution);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
