using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class GeomorphTests
{
    [Fact]
    public void LoadMappings_LoadsVanillaMappings()
    {
        var mappings = GeomorphService.LoadMappings();
        Assert.NotEmpty(mappings);
        Assert.True(mappings.Count >= 180);

        Assert.True(mappings.ContainsKey("geo_32x32_elevator_shaft_dig_site_01.prefab"));
        Assert.Equal("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab",
            mappings["geo_32x32_elevator_shaft_dig_site_01.prefab"]);
    }

    [Fact]
    public void LoadGeomorphs_ParsesLayoutAndMappingsCorrectly()
    {
        var geomorphs = GeomorphService.LoadGeomorphs();
        Assert.NotEmpty(geomorphs);

        // Check complexes
        var complexes = geomorphs.Select(g => g.Complex).Distinct().ToList();
        Assert.Contains("Mining", complexes);
        Assert.Contains("Tech", complexes);
        Assert.Contains("Service", complexes);

        // Check subcomplexes
        var subComplexes = geomorphs.Select(g => g.SubComplex).Distinct().ToList();
        Assert.Contains("Digsite", subComplexes);
        Assert.Contains("Refinery", subComplexes);
        Assert.Contains("Storage", subComplexes);
        Assert.Contains("Data Center", subComplexes);
        Assert.Contains("Labs", subComplexes);
        Assert.Contains("Gardens", subComplexes);
        Assert.Contains("Floodways", subComplexes);

        // Check categories
        var categories = geomorphs.Select(g => g.Category).Distinct().ToList();
        Assert.Contains("Elevators", categories);
        Assert.Contains("Standard", categories);
        Assert.Contains("Custom", categories);

        // Check first elevator item
        var first = geomorphs.First(g => g.Name == "Digsite Elevator 1");
        Assert.Equal("Digsite Elevator 1", first.DisplayName);
        Assert.Equal("geo_32x32_elevator_shaft_dig_site_01.prefab", first.PrefabName);
        Assert.Equal("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab", first.PrefabPath);
        Assert.Equal("img-0001.png", first.ImageName);
        Assert.True(first.HasImage);
        Assert.NotEmpty(first.Description);
    }

    [Fact]
    public void BuildHierarchy_BuildsThreeLevelsCorrectly()
    {
        var geomorphs = GeomorphService.LoadGeomorphs();
        var hierarchy = GeomorphService.BuildHierarchy(geomorphs);

        Assert.Equal(3, hierarchy.Count); // Mining, Tech, Service

        var mining = hierarchy.First(c => c.Name == "Mining");
        Assert.Equal(1, mining.ComplexId);
        Assert.Equal(3, mining.SubComplexes.Count); // Digsite, Refinery, Storage

        var digsite = mining.SubComplexes.First(s => s.Name == "Digsite");
        Assert.Equal(3, digsite.Categories.Count); // Elevators, Standard, Custom

        var elevators = digsite.Categories.First(c => c.Name == "Elevators");
        Assert.NotEmpty(elevators.AllGeomorphs);
        Assert.NotEmpty(elevators.FilteredGeomorphs);
    }

    [Fact]
    public async Task SelectedGeomorph_CopiesPrefabPathToClipboard()
    {
        var mockClipboard = new MockClipboardService();
        var vm = new GeomorphsViewModel(mockClipboard);
        var entry = vm.FilteredAllGeomorphs.First();

        vm.SelectedGeomorph = entry;
        await vm.CopyPrefabPathAsync(entry);

        Assert.Equal(entry.PrefabPath, mockClipboard.LastCopiedText);
        Assert.Contains($"Copied prefab path {entry.PrefabPath} to clipboard", vm.StatusText);
    }

    [Fact]
    public void GeomorphsViewModel_FiltersByName()
    {
        var vm = new GeomorphsViewModel();
        int total = vm.AllGeomorphs.Count;

        vm.SearchText = "Digsite Elevator 1";
        Assert.True(vm.FilteredAllGeomorphs.Count > 0);
        Assert.True(vm.FilteredAllGeomorphs.Count < total);
        Assert.All(vm.FilteredAllGeomorphs, g => Assert.Contains("Digsite Elevator 1", g.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GeomorphsViewModel_FiltersByPrefabName()
    {
        var vm = new GeomorphsViewModel();

        vm.SearchText = "geo_32x32_elevator_shaft_dig_site_01";
        Assert.Single(vm.FilteredAllGeomorphs);
        Assert.Equal("Digsite Elevator 1", vm.FilteredAllGeomorphs[0].Name);
    }

    [Fact]
    public void GeomorphsViewModel_FiltersByComplexAndSubComplex()
    {
        var vm = new GeomorphsViewModel();

        vm.SearchText = "Floodways";
        Assert.NotEmpty(vm.FilteredAllGeomorphs);
        Assert.All(vm.FilteredAllGeomorphs, g => Assert.True(
            g.SubComplex.Contains("Floodways", StringComparison.OrdinalIgnoreCase) ||
            g.Name.Contains("Floodways", StringComparison.OrdinalIgnoreCase) ||
            g.PrefabPath.Contains("Floodways", StringComparison.OrdinalIgnoreCase)
        ));
    }

    [Fact]
    public void GeomorphsViewModel_ClearingSearch_RestoresAll()
    {
        var vm = new GeomorphsViewModel();
        int total = vm.AllGeomorphs.Count;

        vm.SearchText = "Digsite Elevator 1";
        Assert.True(vm.FilteredAllGeomorphs.Count < total);

        vm.SearchText = string.Empty;
        Assert.Equal(total, vm.FilteredAllGeomorphs.Count);
    }

    [Fact]
    public void FormatDisplayName_FormatsCorrectly()
    {
        Assert.Equal("Data Center", GeomorphService.FormatDisplayName("data_center"));
        Assert.Equal("Mining", GeomorphService.FormatDisplayName("mining"));
        Assert.Equal("Digsite Hub Ha 1", GeomorphService.FormatDisplayName("digsite_hub_ha_1"));
    }

    [Fact]
    public void FindImageFile_FindsVanillaImagesOnDisk()
    {
        string? imagePath = GeomorphService.FindImageFile("img-0001.png");
        Assert.NotNull(imagePath);
        Assert.True(File.Exists(imagePath));
        Assert.EndsWith("img-0001.png", imagePath);
    }

    [Fact]
    public void GeomorphEntry_ResolvesImagePathAndBitmap()
    {
        var geomorphs = GeomorphService.LoadGeomorphs();
        var first = geomorphs.First(g => g.Name == "Digsite Elevator 1");

        Assert.Equal("img-0001.png", first.ImageName);
        Assert.True(first.HasImage);
        Assert.NotNull(first.ImagePath);
        Assert.True(File.Exists(first.ImagePath));
    }

    [Fact]
    public void ParseLayoutJson_HandlesCustomJson()
    {
        string json = @"
{
  ""mining"": {
    ""complexId"": 1,
    ""digsite"": {
      ""custom_cat"": [
        {
          ""name"": ""Test Geo"",
          ""description"": ""Item desc"",
          ""variants"": [
            {
              ""prefabs"": [ ""Assets/Custom/geo_test.prefab"" ],
              ""description"": ""Variant desc"",
              ""images"": [ ""test.png"" ]
            }
          ]
        }
      ]
    }
  }
}";
        var entries = GeomorphService.ParseLayoutJson(json);
        Assert.Single(entries);
        var entry = entries[0];
        Assert.Equal("Test Geo", entry.Name);
        Assert.Equal("Test Geo", entry.DisplayName);
        Assert.Equal("Variant desc", entry.Description);
        Assert.Equal("Assets/Custom/geo_test.prefab", entry.PrefabPath);
        Assert.Equal("geo_test.prefab", entry.PrefabName);
        Assert.Equal("test.png", entry.ImageName);
        Assert.Equal("Mining", entry.Complex);
        Assert.Equal(1, entry.ComplexId);
        Assert.Equal("Digsite", entry.SubComplex);
        Assert.Equal("Custom Cat", entry.Category);
    }
}
