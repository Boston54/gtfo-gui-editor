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

public class DimensionConfigTests
{
    [Fact]
    public void DimensionLayoutBlock_Defaults_MatchSpec()
    {
        var dim = new DimensionLayoutBlock();

        // General
        Assert.False(dim.ForbidWaveSpawning);
        Assert.Equal(DimensionLinkedToLayer.Main, dim.LinkedToLayer);
        Assert.Equal(DimensionObjectiveType.Ignore, dim.ObjectiveType);

        // Warp
        Assert.Equal(1998147319u, dim.SoundEventOnWarpTo);
        Assert.False(dim.DisableVFXEventOnWarp);
        Assert.False(dim.ForbidCarryItemWarps);
        Assert.False(dim.LeaveDeployablesOnWarp);

        // Environment
        Assert.Equal(21u, dim.DimensionFogData);
        Assert.Equal(0.0, dim.EnvironmentWetness);
        Assert.NotNull(dim.DustColor);
        Assert.Equal(0.5686275, dim.DustColor.A, 5);
        Assert.Equal(0.496, dim.DustColor.R, 3);
        Assert.Equal(0.496, dim.DustColor.G, 3);
        Assert.Equal(0.496, dim.DustColor.B, 3);
        Assert.Equal(0.0, dim.DustAlphaBoost);
        Assert.Equal(1.0, dim.DustTurbulence);

        // Static Settings
        Assert.True(dim.IsStaticDimension);
        Assert.Equal(0, dim.StaticZoneSeed);
        Assert.Equal(0, dim.StaticMarkerSeed);
        Assert.Equal(-1, dim.StaticAliasOverride);
        Assert.Equal(0u, dim.StaticConsumableDistributionInZone);
        Assert.Empty(dim.StaticBigPickupDistributionInZone);
        Assert.Equal(0, dim.StaticHealthUses);
        Assert.Equal(0, dim.StaticToolUses);
        Assert.Equal(0, dim.StaticAmmoUses);
        Assert.Equal(0, dim.StaticDisinfectUses);
        Assert.Empty(dim.StaticTerminalPlacements);

        // Atmosphere
        Assert.True(dim.IsOutside);
        Assert.Equal(-92.9, dim.LightAzimuth, 1);
        Assert.Equal(30.0, dim.LightElevation, 1);
        Assert.Equal(1.0, dim.LightIntensity, 1);
        Assert.Equal(1.0, dim.AmbientIntensity, 1);
        Assert.Equal(1.0, dim.ReflectionsIntensity, 1);
        Assert.Equal(8000.0, dim.GodrayRange, 1);
        Assert.Equal(1.0, dim.GodrayExponent, 1);
        Assert.NotNull(dim.AtmosphereLightColor);
        Assert.Equal(1.0, dim.AtmosphereLightColor.A);
        Assert.Equal(0.146270916, dim.AtmosphereLightColor.R, 5);
        Assert.Equal(0.1618043, dim.AtmosphereLightColor.G, 5);
        Assert.Equal(0.1792453, dim.AtmosphereLightColor.B, 5);
        Assert.NotNull(dim.AtmospherePlanetAlbedo);
        Assert.Equal(1.0, dim.AtmospherePlanetAlbedo.A);
        Assert.Equal(0.2, dim.AtmospherePlanetAlbedo.R, 2);
        Assert.Equal(0.0627451, dim.AtmospherePlanetAlbedo.G, 5);
        Assert.Equal(0.0, dim.AtmospherePlanetAlbedo.B, 2);
        Assert.Equal(1.0, dim.AtmosphereDensity, 1);
        Assert.Equal(10.0, dim.Exposure, 1);

        // Clouds
        Assert.Equal(DimensionCloudsType.Anvils, dim.CloudsData);
        Assert.Equal(0.5, dim.CloudsCoverage, 1);
        Assert.Equal(0.5, dim.CloudsDensity, 1);
        Assert.Equal(0.0, dim.CloudsSharpness, 1);
        Assert.Equal(1.0, dim.CloudsShadowOpacity, 1);
        Assert.Equal(0.0, dim.CloudsTimescale, 1);
        Assert.Equal(0.1, dim.CloudsCrawling, 1);
        Assert.Equal(0.1, dim.CloudsFade, 1);
        Assert.False(dim.Sandstorm);
        Assert.Equal(0.4, dim.SandstormEdgeA, 1);
        Assert.Equal(0.6, dim.SandstormEdgeB, 1);
        Assert.Equal(0.01, dim.SandstormMinFog, 2);
    }

    [Fact]
    public void DimensionLayoutBlock_CloningAndNotifications_WorkAsExpected()
    {
        var dim = new DimensionLayoutBlock
        {
            DimensionIndex = "Dimension_2",
            Name = "Custom Desert",
            ForbidWaveSpawning = true,
            LinkedToLayer = DimensionLinkedToLayer.Secondary,
            ObjectiveType = DimensionObjectiveType.Independent,
            SoundEventOnWarpTo = 12345,
            DisableVFXEventOnWarp = true,
            ForbidCarryItemWarps = true,
            LeaveDeployablesOnWarp = true,
            DimensionFogData = 40,
            EnvironmentWetness = 0.75,
            DustColor = new LightColor { A = 0.8, R = 0.5, G = 0.4, B = 0.3 },
            DustAlphaBoost = 1.5,
            DustTurbulence = 2.5,
            IsStaticDimension = true,
            StaticZoneSeed = 42,
            StaticMarkerSeed = 99,
            StaticAliasOverride = 5,
            StaticConsumableDistributionInZone = 12,
            StaticHealthUses = 10,
            StaticToolUses = 15,
            StaticAmmoUses = 20,
            StaticDisinfectUses = 25,
            IsOutside = true,
            LightAzimuth = 45.0,
            LightElevation = 60.0,
            LightIntensity = 1.5,
            AmbientIntensity = 0.8,
            ReflectionsIntensity = 0.9,
            GodrayRange = 5000.0,
            GodrayExponent = 2.0,
            AtmosphereLightColor = new LightColor { A = 1.0, R = 0.9, G = 0.8, B = 0.7 },
            AtmospherePlanetAlbedo = new LightColor { A = 1.0, R = 0.1, G = 0.2, B = 0.3 },
            AtmosphereDensity = 2.5,
            Exposure = 5.0,
            CloudsData = DimensionCloudsType.Stratocumulus,
            CloudsCoverage = 0.8,
            CloudsDensity = 0.7,
            CloudsSharpness = 0.3,
            CloudsShadowOpacity = 0.9,
            CloudsTimescale = 0.05,
            CloudsCrawling = 0.2,
            CloudsFade = 0.4,
            Sandstorm = true,
            SandstormEdgeA = 0.35,
            SandstormEdgeB = 0.75,
            SandstormMinFog = 0.02
        };

        dim.StaticBigPickupDistributionInZone.Add(new BigPickupDistributionItem { ItemType = RetrieveBigItemType.PowerCell });
        var tp = new TerminalPlacement { PlacementStartWeight = 1.0, PlacementEndWeight = 0.0 };
        tp.LocalLogFiles.Add(new TerminalLocalLogFile { FileName = "LOG_01", FileContent = "Secret Intel" });
        dim.StaticTerminalPlacements.Add(tp);

        var clone = dim.Clone();

        Assert.Equal(dim.DimensionIndex, clone.DimensionIndex);
        Assert.Equal(dim.Name, clone.Name);
        Assert.True(clone.ForbidWaveSpawning);
        Assert.Equal(DimensionLinkedToLayer.Secondary, clone.LinkedToLayer);
        Assert.Equal(DimensionObjectiveType.Independent, clone.ObjectiveType);
        Assert.Equal(12345u, clone.SoundEventOnWarpTo);
        Assert.True(clone.DisableVFXEventOnWarp);
        Assert.True(clone.ForbidCarryItemWarps);
        Assert.True(clone.LeaveDeployablesOnWarp);
        Assert.Equal(40u, clone.DimensionFogData);
        Assert.Equal(0.75, clone.EnvironmentWetness);
        Assert.Equal(0.8, clone.DustColor.A);
        Assert.Equal(0.5, clone.DustColor.R);
        Assert.Equal(1.5, clone.DustAlphaBoost);
        Assert.Equal(2.5, clone.DustTurbulence);
        Assert.True(clone.IsStaticDimension);
        Assert.Equal(42, clone.StaticZoneSeed);
        Assert.Equal(99, clone.StaticMarkerSeed);
        Assert.Equal(5, clone.StaticAliasOverride);
        Assert.Equal(12u, clone.StaticConsumableDistributionInZone);
        Assert.Equal(10, clone.StaticHealthUses);
        Assert.Equal(15, clone.StaticToolUses);
        Assert.Equal(20, clone.StaticAmmoUses);
        Assert.Equal(25, clone.StaticDisinfectUses);
        Assert.Single(clone.StaticBigPickupDistributionInZone);
        Assert.Equal(RetrieveBigItemType.PowerCell, clone.StaticBigPickupDistributionInZone[0].ItemType);
        Assert.Single(clone.StaticTerminalPlacements);
        Assert.Single(clone.StaticTerminalPlacements[0].LocalLogFiles);
        Assert.Equal("LOG_01", clone.StaticTerminalPlacements[0].LocalLogFiles[0].FileName);
        Assert.Equal("Secret Intel", clone.StaticTerminalPlacements[0].LocalLogFiles[0].FileContent);

        Assert.True(clone.IsOutside);
        Assert.Equal(45.0, clone.LightAzimuth);
        Assert.Equal(60.0, clone.LightElevation);
        Assert.Equal(1.5, clone.LightIntensity);
        Assert.Equal(0.8, clone.AmbientIntensity);
        Assert.Equal(0.9, clone.ReflectionsIntensity);
        Assert.Equal(5000.0, clone.GodrayRange);
        Assert.Equal(2.0, clone.GodrayExponent);
        Assert.Equal(0.9, clone.AtmosphereLightColor.R);
        Assert.Equal(0.1, clone.AtmospherePlanetAlbedo.R);
        Assert.Equal(2.5, clone.AtmosphereDensity);
        Assert.Equal(5.0, clone.Exposure);

        Assert.Equal(DimensionCloudsType.Stratocumulus, clone.CloudsData);
        Assert.Equal(0.8, clone.CloudsCoverage);
        Assert.Equal(0.7, clone.CloudsDensity);
        Assert.Equal(0.3, clone.CloudsSharpness);
        Assert.Equal(0.9, clone.CloudsShadowOpacity);
        Assert.Equal(0.05, clone.CloudsTimescale);
        Assert.Equal(0.2, clone.CloudsCrawling);
        Assert.Equal(0.4, clone.CloudsFade);
        Assert.True(clone.Sandstorm);
        Assert.Equal(0.35, clone.SandstormEdgeA);
        Assert.Equal(0.75, clone.SandstormEdgeB);
        Assert.Equal(0.02, clone.SandstormMinFog);

        // Ensure deep clone independence
        clone.DustColor.R = 0.99;
        Assert.NotEqual(dim.DustColor.R, clone.DustColor.R);
    }

    [Fact]
    public void DimensionExport_FullDatablockGeneration_AndSynthesis()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "DimensionExportTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mainVm = new MainWindowViewModel();
            var level = new LevelLayoutBlock
            {
                Name = "Test_Dimension_Level",
                PersistentId = 1,
                ComplexType = ComplexType.Tech
            };

            var dim = new DimensionLayoutBlock
            {
                DimensionIndex = "Dimension_1",
                Name = "Dimension_Desert_Test",
                DimensionGeomorph = "Assets/AssetPrefabs/Complex/Dimensions/Desert/Dimension_Desert_Static_01.prefab",
                ComplexType = ComplexType.Mining,
                ForbidWaveSpawning = true,
                LinkedToLayer = DimensionLinkedToLayer.Secondary,
                ObjectiveType = DimensionObjectiveType.Independent,
                SoundEventOnWarpTo = 1998147319,
                DisableVFXEventOnWarp = true,
                ForbidCarryItemWarps = true,
                LeaveDeployablesOnWarp = true,
                DimensionFogData = 87,
                EnvironmentWetness = 0.25,
                DustColor = new LightColor { A = 0.7, R = 0.5, G = 0.37, B = 0.25 },
                DustAlphaBoost = 2.0,
                DustTurbulence = 30.0,
                IsStaticDimension = true,
                StaticZoneSeed = 101,
                StaticMarkerSeed = 202,
                StaticAliasOverride = 3,
                StaticConsumableDistributionInZone = 5,
                StaticHealthUses = 4,   // 4 * 0.2 = 0.8
                StaticToolUses = 6,     // 6 * 0.2 = 1.2
                StaticAmmoUses = 8,     // 8 * 0.2 = 1.6
                StaticDisinfectUses = 2, // 2 * 0.2 = 0.4
                IsOutside = true,
                LightAzimuth = 170.8,
                LightElevation = 28.1,
                LightIntensity = 0.5,
                AmbientIntensity = 0.7,
                ReflectionsIntensity = 1.0,
                GodrayRange = 4500.0,
                GodrayExponent = 2.5,
                AtmosphereLightColor = new LightColor { A = 1.0, R = 0.15, G = 0.16, B = 0.18 },
                AtmospherePlanetAlbedo = new LightColor { A = 1.0, R = 0.25, G = 0.08, B = 0.02 },
                AtmosphereDensity = 4.0,
                Exposure = 4.0,
                CloudsData = DimensionCloudsType.Anvils,
                CloudsCoverage = 0.962,
                CloudsDensity = 1.0,
                CloudsSharpness = 0.5,
                CloudsShadowOpacity = 1.0,
                CloudsTimescale = 0.0,
                CloudsCrawling = -0.02,
                CloudsFade = 0.35,
                Sandstorm = true,
                SandstormEdgeA = 0.25,
                SandstormEdgeB = 1.0,
                SandstormMinFog = 0.006
            };

            dim.StaticBigPickupDistributionInZone.Add(new BigPickupDistributionItem { ItemType = RetrieveBigItemType.PowerCell });
            dim.StaticBigPickupDistributionInZone.Add(new BigPickupDistributionItem { ItemType = RetrieveBigItemType.FogTurbine });

            var tp = new TerminalPlacement
            {
                PlacementStartWeight = 0.5,
                PlacementMiddleWeight = 0.3,
                PlacementEndWeight = 0.2,
                AreaSeedOffset = 10,
                MarkerSeedOffset = 126,
                StartingState = TerminalStartingState.Sleeping,
                PasswordProtected = true,
                PasswordHintText = "Desert Code Required",
                GeneratePassword = true,
                PasswordPartCount = 2
            };
            tp.LocalLogFiles.Add(new TerminalLocalLogFile
            {
                FileName = "DESERT-LOG-01",
                FileContent = "Warped into extreme desert heat."
            });
            dim.StaticTerminalPlacements.Add(tp);

            level.SetDimension(dim);
            mainVm.LevelLayout.Levels.Clear();
            mainVm.LevelLayout.Levels.Add(level);

            DatablockExportService.ExportAll(mainVm, tempDir);

            // 1. Verify DimensionDataBlock export
            string dimDbPath = Path.Combine(tempDir, "GameData_DimensionDataBlock_bin.json");
            Assert.True(File.Exists(dimDbPath));
            string dimJson = File.ReadAllText(dimDbPath);
            using var dimDoc = JsonDocument.Parse(dimJson);
            var dimBlocks = dimDoc.RootElement.GetProperty("Blocks");
            Assert.True(dimBlocks.GetArrayLength() > 0);

            var dimBlock = dimBlocks.EnumerateArray().FirstOrDefault(b => b.GetProperty("name").GetString() == "Dimension_Desert_Test");
            Assert.Equal(JsonValueKind.Object, dimBlock.ValueKind);

            var dimData = dimBlock.GetProperty("DimensionData");
            Assert.True(dimData.GetProperty("ForbidWaveSpawning").GetBoolean());
            Assert.Equal("SecondaryLayer", dimData.GetProperty("LinkedToLayer").GetString());
            Assert.Equal("Independent", dimData.GetProperty("ObjectiveType").GetString());

            Assert.Equal(1998147319u, dimData.GetProperty("SoundEventOnWarpTo").GetUInt32());
            Assert.True(dimData.GetProperty("DisableVFXEventOnWarp").GetBoolean());
            Assert.True(dimData.GetProperty("ForbidCarryItemWarps").GetBoolean());
            Assert.True(dimData.GetProperty("LeaveDeployablesOnWarp").GetBoolean());

            Assert.Equal(87u, dimData.GetProperty("DimensionFogData").GetUInt32());
            Assert.Equal(0.25, dimData.GetProperty("EnvironmentWetness").GetDouble(), 2);
            var dustColor = dimData.GetProperty("DustColor");
            Assert.Equal(0.7, dustColor.GetProperty("a").GetDouble(), 2);
            Assert.Equal(0.5, dustColor.GetProperty("r").GetDouble(), 2);
            Assert.Equal(0.37, dustColor.GetProperty("g").GetDouble(), 2);
            Assert.Equal(0.25, dustColor.GetProperty("b").GetDouble(), 2);
            Assert.Equal(2.0, dimData.GetProperty("DustAlphaBoost").GetDouble(), 1);
            Assert.Equal(30.0, dimData.GetProperty("DustTurbulence").GetDouble(), 1);

            Assert.True(dimData.GetProperty("IsStaticDimension").GetBoolean());
            Assert.Equal(101, dimData.GetProperty("StaticZoneSeed").GetInt32());
            Assert.Equal(202, dimData.GetProperty("StaticMarkerSeed").GetInt32());
            Assert.Equal(3, dimData.GetProperty("StaticAliasOverride").GetInt32());
            Assert.Equal(5u, dimData.GetProperty("StaticConsumableDistributionInZone").GetUInt32());
            Assert.True(dimData.GetProperty("StaticAllowSmallPickupsAllocation").GetBoolean());
            Assert.True(dimData.GetProperty("StaticForceBigPickupsAllocation").GetBoolean());
            Assert.Equal(2, dimData.GetProperty("StaticBigPickupDistributionInZone").GetInt32());

            Assert.Equal(0.8, dimData.GetProperty("StaticHealthMulti").GetDouble(), 2);
            Assert.Equal(1.2, dimData.GetProperty("StaticToolAmmoMulti").GetDouble(), 2);
            Assert.Equal(1.6, dimData.GetProperty("StaticWeaponAmmoMulti").GetDouble(), 2);
            Assert.Equal(0.4, dimData.GetProperty("StaticDisinfectionMulti").GetDouble(), 2);

            var termPlacements = dimData.GetProperty("StaticTerminalPlacements");
            Assert.Equal(1, termPlacements.GetArrayLength());
            var firstTerm = termPlacements[0];
            Assert.Equal(10, firstTerm.GetProperty("AreaSeedOffset").GetInt32());
            Assert.Equal(126, firstTerm.GetProperty("MarkerSeedOffset").GetInt32());
            var localLogs = firstTerm.GetProperty("LocalLogFiles");
            Assert.Equal(1, localLogs.GetArrayLength());
            Assert.Equal("DESERT-LOG-01", localLogs[0].GetProperty("FileName").GetString());
            Assert.True(localLogs[0].GetProperty("FileContent").GetUInt32() > 0);

            Assert.True(dimData.GetProperty("IsOutside").GetBoolean());
            Assert.Equal(170.8, dimData.GetProperty("LightAzimuth").GetDouble(), 1);
            Assert.Equal(28.1, dimData.GetProperty("LightElevation").GetDouble(), 1);
            Assert.Equal(0.5, dimData.GetProperty("LightIntensity").GetDouble(), 1);
            Assert.Equal(0.7, dimData.GetProperty("AmbientIntensity").GetDouble(), 1);
            Assert.Equal(1.0, dimData.GetProperty("ReflectionsIntensity").GetDouble(), 1);
            Assert.Equal(4500.0, dimData.GetProperty("GodrayRange").GetDouble(), 1);
            Assert.Equal(2.5, dimData.GetProperty("GodrayExponent").GetDouble(), 1);
            Assert.Equal(4.0, dimData.GetProperty("AtmosphereDensity").GetDouble(), 1);
            Assert.Equal(4.0, dimData.GetProperty("Exposure").GetDouble(), 1);

            Assert.Equal(1, dimData.GetProperty("CloudsData").GetInt32());
            Assert.Equal(0.962, dimData.GetProperty("CloudsCoverage").GetDouble(), 3);
            Assert.Equal(1.0, dimData.GetProperty("CloudsDensity").GetDouble(), 1);
            Assert.Equal(0.5, dimData.GetProperty("CloudsSharpness").GetDouble(), 1);
            Assert.Equal(1.0, dimData.GetProperty("CloudsShadowOpacity").GetDouble(), 1);
            Assert.Equal(0.0, dimData.GetProperty("CloudsTimescale").GetDouble(), 1);
            Assert.Equal(-0.02, dimData.GetProperty("CloudsCrawling").GetDouble(), 2);
            Assert.Equal(0.35, dimData.GetProperty("CloudsFade").GetDouble(), 2);
            Assert.True(dimData.GetProperty("Sandstorm").GetBoolean());
            Assert.Equal(0.25, dimData.GetProperty("SandstormEdgeA").GetDouble(), 2);
            Assert.Equal(1.0, dimData.GetProperty("SandstormEdgeB").GetDouble(), 1);
            Assert.Equal(0.006, dimData.GetProperty("SandstormMinFog").GetDouble(), 3);

            // 2. Verify AtmosphereDataBlock export and synthesis
            uint atmoDataId = dimData.GetProperty("AtmosphereData").GetUInt32();
            Assert.True(atmoDataId > 0);

            string atmoDbPath = Path.Combine(tempDir, "GameData_AtmosphereDataBlock_bin.json");
            Assert.True(File.Exists(atmoDbPath));
            string atmoJson = File.ReadAllText(atmoDbPath);
            using var atmoDoc = JsonDocument.Parse(atmoJson);
            var atmoBlocks = atmoDoc.RootElement.GetProperty("Blocks");
            // Must contain vanilla atmosphere entries (e.g. ID 1, 2)
            Assert.True(atmoBlocks.GetArrayLength() > 2);

            var synthesizedAtmo = atmoBlocks.EnumerateArray().FirstOrDefault(b => b.GetProperty("persistentID").GetUInt32() == atmoDataId);
            Assert.Equal(JsonValueKind.Object, synthesizedAtmo.ValueKind);
            var lightCol = synthesizedAtmo.GetProperty("LightColor");
            Assert.Equal(0.15, lightCol.GetProperty("r").GetDouble(), 2);
            Assert.Equal(0.16, lightCol.GetProperty("g").GetDouble(), 2);
            Assert.Equal(0.18, lightCol.GetProperty("b").GetDouble(), 2);
            var planetAlb = synthesizedAtmo.GetProperty("PlanetAlbedo");
            Assert.Equal(0.25, planetAlb.GetProperty("r").GetDouble(), 2);
            Assert.Equal(0.08, planetAlb.GetProperty("g").GetDouble(), 2);
            Assert.Equal(0.02, planetAlb.GetProperty("b").GetDouble(), 2);

            // 3. Verify ComplexResourceSetDataBlock synthesis for dimension
            uint dimCrsId = dimData.GetProperty("DimensionResourceSetID").GetUInt32();
            Assert.True(dimCrsId > 0);

            string crsDbPath = Path.Combine(tempDir, "GameData_ComplexResourceSetDataBlock_bin.json");
            Assert.True(File.Exists(crsDbPath));
            string crsJson = File.ReadAllText(crsDbPath);
            using var crsDoc = JsonDocument.Parse(crsJson);
            var crsBlocks = crsDoc.RootElement.GetProperty("Blocks");
            var dimCrsBlock = crsBlocks.EnumerateArray().FirstOrDefault(b => b.GetProperty("persistentID").GetUInt32() == dimCrsId);
            Assert.Equal(JsonValueKind.Object, dimCrsBlock.ValueKind);
            Assert.Equal("Generated_ComplexSet_Test_Dimension_Level_Dimension_Desert_Test", dimCrsBlock.GetProperty("name").GetString());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void LevelLayoutViewModel_DimensionDialog_OpenConfirmCancelAndCommands()
    {
        var vm = new LevelLayoutViewModel();
        var level = new LevelLayoutBlock { Name = "VM_Dim_Level", PersistentId = 10 };
        vm.Levels.Add(level);
        vm.SelectedLevel = level;

        var dimOpt = new DimensionOption("Dimension_1", "Dim One");
        vm.AvailableDimensions.Add(dimOpt);

        vm.OpenPromptDimensionDialog(dimOpt);
        Assert.True(vm.IsConfiguringDimension);
        Assert.NotNull(vm.EditingDimension);
        Assert.Equal("Dimension_1", vm.EditingDimension.DimensionIndex);

        // Edit fields on EditingDimension
        vm.EditingDimension.ForbidWaveSpawning = true;
        vm.EditingDimension.LinkedToLayer = DimensionLinkedToLayer.Overload;
        vm.EditingDimension.ObjectiveType = DimensionObjectiveType.Independent;
        vm.EditingDimension.IsStaticDimension = true;
        vm.EditingDimension.StaticZoneSeed = 999;
        vm.EditingDimension.StaticHealthUses = 5;

        // Commands on EditingDimension
        Assert.Empty(vm.EditingDimension.StaticBigPickupDistributionInZone);
        vm.AddDimensionBigPickupDistributionItem();
        Assert.Single(vm.EditingDimension.StaticBigPickupDistributionInZone);
        var bigItem = vm.EditingDimension.StaticBigPickupDistributionInZone[0];
        vm.RemoveDimensionBigPickupDistributionItem(bigItem);
        Assert.Empty(vm.EditingDimension.StaticBigPickupDistributionInZone);

        Assert.Empty(vm.EditingDimension.StaticTerminalPlacements);
        vm.AddDimensionTerminalPlacement();
        Assert.Single(vm.EditingDimension.StaticTerminalPlacements);
        var term = vm.EditingDimension.StaticTerminalPlacements[0];
        term.AddLocalLogFile();
        Assert.Single(term.LocalLogFiles);
        var log = term.LocalLogFiles[0];
        log.FileName = "TEST_LOG";
        log.FileContent = "Sample Content";

        // Remove log file command
        vm.RemoveTerminalLocalLogFile(log);
        Assert.Empty(term.LocalLogFiles);

        // Confirm dialog
        vm.SelectedDimensionTile = new ElevatorTileOption { Prefab = "Assets/Scenes/MPTests/HuntingGroupTest/Dimension_Test.prefab" };
        bool confirmed = vm.ConfirmPromptDimension();
        Assert.True(confirmed);
        Assert.False(vm.IsConfiguringDimension);
        Assert.Null(vm.EditingDimension);

        var savedDim = level.GetDimension("Dimension_1");
        Assert.NotNull(savedDim);
        Assert.True(savedDim.ForbidWaveSpawning);
        Assert.Equal(DimensionLinkedToLayer.Overload, savedDim.LinkedToLayer);
        Assert.Equal(DimensionObjectiveType.Independent, savedDim.ObjectiveType);
        Assert.Equal(999, savedDim.StaticZoneSeed);
        Assert.Equal(5, savedDim.StaticHealthUses);
    }

    [Fact]
    public void DimensionProjectFile_SaveAndLoad_PreservesAllFields()
    {
        var project = new RundownProjectFile();
        var level = new LevelLayoutBlock
        {
            Name = "Dim_SaveLoad_Level",
            PersistentId = 444,
            ComplexType = ComplexType.Service
        };

        var dim = new DimensionLayoutBlock
        {
            DimensionIndex = "Dimension_1",
            Name = "Saved_Desert",
            DimensionGeomorph = "Assets/Scenes/MPTests/HuntingGroupTest/Dimension_Test.prefab",
            ComplexType = ComplexType.Service,
            ForbidWaveSpawning = true,
            LinkedToLayer = DimensionLinkedToLayer.Secondary,
            ObjectiveType = DimensionObjectiveType.LinkedToLayer,
            SoundEventOnWarpTo = 98765,
            DisableVFXEventOnWarp = true,
            ForbidCarryItemWarps = true,
            LeaveDeployablesOnWarp = true,
            DimensionFogData = 55,
            EnvironmentWetness = 0.65,
            DustColor = new LightColor { A = 0.9, R = 0.4, G = 0.3, B = 0.2 },
            DustAlphaBoost = 1.8,
            DustTurbulence = 12.0,
            IsStaticDimension = true,
            StaticZoneSeed = 77,
            StaticMarkerSeed = 88,
            StaticAliasOverride = 4,
            StaticConsumableDistributionInZone = 9,
            StaticHealthUses = 3,
            StaticToolUses = 7,
            StaticAmmoUses = 11,
            StaticDisinfectUses = 2,
            IsOutside = true,
            LightAzimuth = 120.0,
            LightElevation = 40.0,
            LightIntensity = 1.2,
            AmbientIntensity = 0.9,
            ReflectionsIntensity = 0.8,
            GodrayRange = 6000.0,
            GodrayExponent = 1.5,
            AtmosphereLightColor = new LightColor { A = 1.0, R = 0.8, G = 0.7, B = 0.6 },
            AtmospherePlanetAlbedo = new LightColor { A = 1.0, R = 0.3, G = 0.2, B = 0.1 },
            AtmosphereDensity = 3.0,
            Exposure = 8.0,
            CloudsData = DimensionCloudsType.Stratocumulus,
            CloudsCoverage = 0.75,
            CloudsDensity = 0.85,
            CloudsSharpness = 0.2,
            CloudsShadowOpacity = 0.95,
            CloudsTimescale = 0.03,
            CloudsCrawling = 0.15,
            CloudsFade = 0.25,
            Sandstorm = true,
            SandstormEdgeA = 0.3,
            SandstormEdgeB = 0.7,
            SandstormMinFog = 0.015
        };

        dim.StaticBigPickupDistributionInZone.Add(new BigPickupDistributionItem { ItemType = RetrieveBigItemType.CryoHardcase });
        var tp = new TerminalPlacement
        {
            PlacementStartWeight = 0.8,
            PlacementEndWeight = 0.2,
            AreaSeedOffset = 5,
            StartingState = TerminalStartingState.Sleeping,
            PasswordProtected = true,
            PasswordHintText = "Saved Code"
        };
        tp.LocalLogFiles.Add(new TerminalLocalLogFile { FileName = "SAVE_LOG_1", FileContent = "Saved Content" });
        dim.StaticTerminalPlacements.Add(tp);

        var levelData = new LevelLayoutBlockData
        {
            Name = level.Name,
            PersistentId = level.PersistentId,
            ComplexType = level.ComplexType,
            Dimensions = new List<DimensionLayoutBlock> { dim }
        };
        project.Levels.Add(levelData);

        string json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<RundownProjectFile>(json);

        Assert.NotNull(deserialized);
        var loadedLevel = deserialized.Levels.FirstOrDefault(l => l.PersistentId == 444);
        Assert.NotNull(loadedLevel);

        var loadedDim = loadedLevel.Dimensions.FirstOrDefault(d => d.DimensionIndex == "Dimension_1");
        Assert.NotNull(loadedDim);
        Assert.Equal("Saved_Desert", loadedDim.Name);
        Assert.True(loadedDim.ForbidWaveSpawning);
        Assert.Equal(DimensionLinkedToLayer.Secondary, loadedDim.LinkedToLayer);
        Assert.Equal(DimensionObjectiveType.LinkedToLayer, loadedDim.ObjectiveType);
        Assert.Equal(98765u, loadedDim.SoundEventOnWarpTo);
        Assert.True(loadedDim.DisableVFXEventOnWarp);
        Assert.True(loadedDim.ForbidCarryItemWarps);
        Assert.True(loadedDim.LeaveDeployablesOnWarp);
        Assert.Equal(55u, loadedDim.DimensionFogData);
        Assert.Equal(0.65, loadedDim.EnvironmentWetness);
        Assert.Equal(0.9, loadedDim.DustColor.A);
        Assert.Equal(0.4, loadedDim.DustColor.R);
        Assert.Equal(1.8, loadedDim.DustAlphaBoost);
        Assert.Equal(12.0, loadedDim.DustTurbulence);
        Assert.True(loadedDim.IsStaticDimension);
        Assert.Equal(77, loadedDim.StaticZoneSeed);
        Assert.Equal(88, loadedDim.StaticMarkerSeed);
        Assert.Equal(4, loadedDim.StaticAliasOverride);
        Assert.Equal(9u, loadedDim.StaticConsumableDistributionInZone);
        Assert.Equal(3, loadedDim.StaticHealthUses);
        Assert.Equal(7, loadedDim.StaticToolUses);
        Assert.Equal(11, loadedDim.StaticAmmoUses);
        Assert.Equal(2, loadedDim.StaticDisinfectUses);
        Assert.Single(loadedDim.StaticBigPickupDistributionInZone);
        Assert.Equal(RetrieveBigItemType.CryoHardcase, loadedDim.StaticBigPickupDistributionInZone[0].ItemType);
        Assert.Single(loadedDim.StaticTerminalPlacements);
        Assert.Equal(5, loadedDim.StaticTerminalPlacements[0].AreaSeedOffset);
        Assert.Single(loadedDim.StaticTerminalPlacements[0].LocalLogFiles);
        Assert.Equal("SAVE_LOG_1", loadedDim.StaticTerminalPlacements[0].LocalLogFiles[0].FileName);
        Assert.Equal("Saved Content", loadedDim.StaticTerminalPlacements[0].LocalLogFiles[0].FileContent);

        Assert.True(loadedDim.IsOutside);
        Assert.Equal(120.0, loadedDim.LightAzimuth);
        Assert.Equal(40.0, loadedDim.LightElevation);
        Assert.Equal(1.2, loadedDim.LightIntensity);
        Assert.Equal(0.9, loadedDim.AmbientIntensity);
        Assert.Equal(0.8, loadedDim.ReflectionsIntensity);
        Assert.Equal(6000.0, loadedDim.GodrayRange);
        Assert.Equal(1.5, loadedDim.GodrayExponent);
        Assert.Equal(0.8, loadedDim.AtmosphereLightColor.R);
        Assert.Equal(0.3, loadedDim.AtmospherePlanetAlbedo.R);
        Assert.Equal(3.0, loadedDim.AtmosphereDensity);
        Assert.Equal(8.0, loadedDim.Exposure);

        Assert.Equal(DimensionCloudsType.Stratocumulus, loadedDim.CloudsData);
        Assert.Equal(0.75, loadedDim.CloudsCoverage);
        Assert.Equal(0.85, loadedDim.CloudsDensity);
        Assert.Equal(0.2, loadedDim.CloudsSharpness);
        Assert.Equal(0.95, loadedDim.CloudsShadowOpacity);
        Assert.Equal(0.03, loadedDim.CloudsTimescale);
        Assert.Equal(0.15, loadedDim.CloudsCrawling);
        Assert.Equal(0.25, loadedDim.CloudsFade);
        Assert.True(loadedDim.Sandstorm);
        Assert.Equal(0.3, loadedDim.SandstormEdgeA);
        Assert.Equal(0.7, loadedDim.SandstormEdgeB);
        Assert.Equal(0.015, loadedDim.SandstormMinFog);
    }

    [Fact]
    public void DimensionExportAndImport_Roundtrip_RestoresAllFields()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "DimensionRoundtripTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var exportVm = new MainWindowViewModel();
            var level = new LevelLayoutBlock
            {
                Name = "Roundtrip_Level",
                PersistentId = 555,
                ComplexType = ComplexType.Tech
            };

            var dim = new DimensionLayoutBlock
            {
                DimensionIndex = "Dimension_1",
                Name = "Roundtrip_Dimension",
                DimensionGeomorph = "Assets/AssetPrefabs/Complex/Dimensions/Desert/Dimension_Desert_Static_01.prefab",
                ComplexType = ComplexType.Tech,
                ForbidWaveSpawning = true,
                LinkedToLayer = DimensionLinkedToLayer.Main,
                ObjectiveType = DimensionObjectiveType.Ignore,
                SoundEventOnWarpTo = 1998147319,
                DisableVFXEventOnWarp = true,
                ForbidCarryItemWarps = true,
                LeaveDeployablesOnWarp = true,
                DimensionFogData = 87,
                EnvironmentWetness = 0.35,
                DustColor = new LightColor { A = 0.6, R = 0.5, G = 0.4, B = 0.3 },
                DustAlphaBoost = 1.5,
                DustTurbulence = 20.0,
                IsStaticDimension = true,
                StaticZoneSeed = 111,
                StaticMarkerSeed = 222,
                StaticAliasOverride = 7,
                StaticConsumableDistributionInZone = 8,
                StaticHealthUses = 5,
                StaticToolUses = 10,
                StaticAmmoUses = 15,
                StaticDisinfectUses = 20,
                IsOutside = true,
                LightAzimuth = 100.0,
                LightElevation = 35.0,
                LightIntensity = 1.1,
                AmbientIntensity = 0.6,
                ReflectionsIntensity = 0.7,
                GodrayRange = 7000.0,
                GodrayExponent = 1.8,
                AtmosphereLightColor = new LightColor { A = 1.0, R = 0.2, G = 0.3, B = 0.4 },
                AtmospherePlanetAlbedo = new LightColor { A = 1.0, R = 0.5, G = 0.4, B = 0.3 },
                AtmosphereDensity = 2.0,
                Exposure = 9.0,
                CloudsData = DimensionCloudsType.Anvils,
                CloudsCoverage = 0.6,
                CloudsDensity = 0.7,
                CloudsSharpness = 0.1,
                CloudsShadowOpacity = 0.8,
                CloudsTimescale = 0.02,
                CloudsCrawling = 0.05,
                CloudsFade = 0.15,
                Sandstorm = true,
                SandstormEdgeA = 0.3,
                SandstormEdgeB = 0.8,
                SandstormMinFog = 0.02
            };

            var tp = new TerminalPlacement
            {
                PlacementStartWeight = 0.7,
                PlacementEndWeight = 0.3,
                AreaSeedOffset = 15,
                MarkerSeedOffset = 50,
                StartingState = TerminalStartingState.Sleeping,
                PasswordProtected = true,
                PasswordHintText = "Import Password Hint",
                GeneratePassword = true,
                PasswordPartCount = 3
            };
            tp.LocalLogFiles.Add(new TerminalLocalLogFile
            {
                FileName = "IMPORT_LOG_TEST",
                FileContent = "Import Log Content"
            });
            dim.StaticTerminalPlacements.Add(tp);

            level.SetDimension(dim);
            exportVm.LevelLayout.Levels.Clear();
            exportVm.LevelLayout.Levels.Add(level);

            DatablockExportService.ExportAll(exportVm, tempDir);

            // Import back into a fresh view model
            var importVm = new MainWindowViewModel();
            var importManager = new DatablockManager();
            var result = importManager.ImportDatablocks(importVm, tempDir);
            Assert.True(result.Success, result.Message);

            var importedLevel = importVm.LevelLayout.Levels.FirstOrDefault(l => l.Name == "Roundtrip_Level" || l.PersistentId == 555);
            Assert.NotNull(importedLevel);

            var importedDim = importedLevel.GetDimension("Dimension_1");
            Assert.NotNull(importedDim);
            Assert.Equal("Roundtrip_Dimension", importedDim.Name);
            Assert.True(importedDim.ForbidWaveSpawning);
            Assert.Equal(DimensionLinkedToLayer.Main, importedDim.LinkedToLayer);
            Assert.Equal(DimensionObjectiveType.Ignore, importedDim.ObjectiveType);
            Assert.Equal(1998147319u, importedDim.SoundEventOnWarpTo);
            Assert.True(importedDim.DisableVFXEventOnWarp);
            Assert.True(importedDim.ForbidCarryItemWarps);
            Assert.True(importedDim.LeaveDeployablesOnWarp);
            Assert.Equal(87u, importedDim.DimensionFogData);
            Assert.Equal(0.35, importedDim.EnvironmentWetness, 2);
            Assert.Equal(0.6, importedDim.DustColor.A, 1);
            Assert.Equal(0.5, importedDim.DustColor.R, 1);
            Assert.Equal(1.5, importedDim.DustAlphaBoost, 1);
            Assert.Equal(20.0, importedDim.DustTurbulence, 1);

            Assert.True(importedDim.IsStaticDimension);
            Assert.Equal(111, importedDim.StaticZoneSeed);
            Assert.Equal(222, importedDim.StaticMarkerSeed);
            Assert.Equal(7, importedDim.StaticAliasOverride);
            Assert.Equal(8u, importedDim.StaticConsumableDistributionInZone);
            Assert.Equal(5, importedDim.StaticHealthUses);
            Assert.Equal(10, importedDim.StaticToolUses);
            Assert.Equal(15, importedDim.StaticAmmoUses);
            Assert.Equal(20, importedDim.StaticDisinfectUses);

            Assert.Single(importedDim.StaticTerminalPlacements);
            var importedTp = importedDim.StaticTerminalPlacements[0];
            Assert.Equal(15, importedTp.AreaSeedOffset);
            Assert.Equal(50, importedTp.MarkerSeedOffset);
            Assert.True(importedTp.PasswordProtected);
            Assert.Equal("Import Password Hint", importedTp.PasswordHintText);
            Assert.True(importedTp.GeneratePassword);
            Assert.Equal(3, importedTp.PasswordPartCount);
            Assert.Single(importedTp.LocalLogFiles);
            Assert.Equal("IMPORT_LOG_TEST", importedTp.LocalLogFiles[0].FileName);
            Assert.Equal("Import Log Content", importedTp.LocalLogFiles[0].FileContent);

            Assert.True(importedDim.IsOutside);
            Assert.Equal(100.0, importedDim.LightAzimuth, 1);
            Assert.Equal(35.0, importedDim.LightElevation, 1);
            Assert.Equal(1.1, importedDim.LightIntensity, 1);
            Assert.Equal(0.6, importedDim.AmbientIntensity, 1);
            Assert.Equal(0.7, importedDim.ReflectionsIntensity, 1);
            Assert.Equal(7000.0, importedDim.GodrayRange, 1);
            Assert.Equal(1.8, importedDim.GodrayExponent, 1);
            Assert.Equal(2.0, importedDim.AtmosphereDensity, 1);
            Assert.Equal(9.0, importedDim.Exposure, 1);

            Assert.Equal(0.2, importedDim.AtmosphereLightColor.R, 1);
            Assert.Equal(0.3, importedDim.AtmosphereLightColor.G, 1);
            Assert.Equal(0.4, importedDim.AtmosphereLightColor.B, 1);
            Assert.Equal(0.5, importedDim.AtmospherePlanetAlbedo.R, 1);
            Assert.Equal(0.4, importedDim.AtmospherePlanetAlbedo.G, 1);
            Assert.Equal(0.3, importedDim.AtmospherePlanetAlbedo.B, 1);

            Assert.Equal(DimensionCloudsType.Anvils, importedDim.CloudsData);
            Assert.Equal(0.6, importedDim.CloudsCoverage, 1);
            Assert.Equal(0.7, importedDim.CloudsDensity, 1);
            Assert.Equal(0.1, importedDim.CloudsSharpness, 1);
            Assert.Equal(0.8, importedDim.CloudsShadowOpacity, 1);
            Assert.Equal(0.02, importedDim.CloudsTimescale, 2);
            Assert.Equal(0.05, importedDim.CloudsCrawling, 2);
            Assert.Equal(0.15, importedDim.CloudsFade, 2);
            Assert.True(importedDim.Sandstorm);
            Assert.Equal(0.3, importedDim.SandstormEdgeA, 1);
            Assert.Equal(0.8, importedDim.SandstormEdgeB, 1);
            Assert.Equal(0.02, importedDim.SandstormMinFog, 2);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void EditDimension_And_NewDimension_ProvideIdenticalEditingDimensionState()
    {
        var level = new LevelLayoutBlock
        {
            Name = "DimensionTestLevel",
            PersistentId = 1,
            ComplexType = ComplexType.Mining,
            ElevatorTile = "Assets/Elevators/Shaft1.prefab"
        };

        var vm = new LevelLayoutViewModel(new System.Collections.ObjectModel.ObservableCollection<ComplexResourceSetBlock>(), new List<LevelLayoutBlock> { level });

        // 1. Edit Reality Dimension
        vm.RequestEditActiveDimension();
        Assert.True(vm.IsConfiguringDimension);
        Assert.NotNull(vm.EditingDimension);
        Assert.Equal("Reality", vm.EditingDimension.DimensionIndex);
        Assert.Equal(ComplexType.Mining, vm.EditingDimension.ComplexType);
        Assert.Equal(1998147319u, vm.EditingDimension.SoundEventOnWarpTo);
        Assert.True(vm.EditingDimension.IsStaticDimension);
        Assert.True(vm.EditingDimension.IsOutside);
        vm.CancelPromptDimension();
        Assert.False(vm.IsConfiguringDimension);
        Assert.Null(vm.EditingDimension);

        // 2. Add New Dimension
        vm.OpenAddDimensionDialog();
        Assert.True(vm.IsConfiguringDimension);
        Assert.NotNull(vm.EditingDimension);
        Assert.Equal("Dimension_1", vm.EditingDimension.DimensionIndex);
        Assert.Equal(1998147319u, vm.EditingDimension.SoundEventOnWarpTo);
        Assert.True(vm.EditingDimension.IsStaticDimension);
        Assert.True(vm.EditingDimension.IsOutside);

        // Modify and confirm
        vm.EditingDimension.SoundEventOnWarpTo = 54321;
        vm.EditingDimension.ForbidWaveSpawning = true;
        vm.EditingDimension.EnvironmentWetness = 0.8;
        vm.ConfirmPromptDimension();
        Assert.False(vm.IsConfiguringDimension);

        // 3. Edit Non-Reality Dimension
        vm.SelectedDimension = vm.AvailableDimensions.FirstOrDefault(d => d.DimensionIndex == "Dimension_1");
        vm.RequestEditActiveDimension();
        Assert.True(vm.IsConfiguringDimension);
        Assert.NotNull(vm.EditingDimension);
        Assert.Equal("Dimension_1", vm.EditingDimension.DimensionIndex);
        Assert.Equal(54321u, vm.EditingDimension.SoundEventOnWarpTo);
        Assert.True(vm.EditingDimension.ForbidWaveSpawning);
        Assert.Equal(0.8, vm.EditingDimension.EnvironmentWetness);
        vm.CancelPromptDimension();
        Assert.False(vm.IsConfiguringDimension);
    }

    [Fact]
    public void LevelLayoutBlock_SnatcherBoxes_PropertyAndCloning_Works()
    {
        var level = new LevelLayoutBlock();
        Assert.False(level.SnatcherBoxes);

        level.SnatcherBoxes = true;
        Assert.True(level.SnatcherBoxes);

        var clone = level.Clone();
        Assert.True(clone.SnatcherBoxes);

        clone.SnatcherBoxes = false;
        Assert.False(clone.SnatcherBoxes);
        Assert.True(level.SnatcherBoxes);
    }

    [Fact]
    public void LevelLayoutViewModel_SnatcherBoxes_CreateAndEditLevel_Works()
    {
        var level = new LevelLayoutBlock
        {
            Name = "Original Level",
            PersistentId = 101,
            ComplexType = ComplexType.Mining,
            ElevatorTile = "Assets/Elevators/Shaft1.prefab",
            SnatcherBoxes = false
        };

        var vm = new LevelLayoutViewModel(new System.Collections.ObjectModel.ObservableCollection<ComplexResourceSetBlock>(), new List<LevelLayoutBlock> { level });

        // Edit level and set SnatcherBoxes = true
        vm.SelectedLevel = level;
        vm.OpenEditLevelDialog();
        Assert.True(vm.IsCreatingLevel);
        Assert.True(vm.IsEditingExistingLevel);
        Assert.False(vm.NewLevelSnatcherBoxes);

        vm.NewLevelSnatcherBoxes = true;
        bool confirmed = vm.ConfirmCreateLevel();
        Assert.True(confirmed);
        Assert.False(vm.IsCreatingLevel);
        Assert.True(level.SnatcherBoxes);

        // Create new level with SnatcherBoxes
        vm.OpenCreateLevelDialog();
        Assert.True(vm.IsCreatingLevel);
        Assert.False(vm.IsEditingExistingLevel);
        vm.NewLevelName = "New Snatcher Level";
        vm.NewLevelPersistentId = 102;
        vm.NewLevelSnatcherBoxes = true;

        confirmed = vm.ConfirmCreateLevel();
        Assert.True(confirmed);
        var created = vm.Levels.FirstOrDefault(l => l.PersistentId == 102);
        Assert.NotNull(created);
        Assert.True(created.SnatcherBoxes);
    }

    [Fact]
    public void RundownLoader_SnatcherBoxes_SaveAndLoad_PreservesState()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"gtfo_test_snatcher_{Guid.NewGuid():N}.rundown");
        try
        {
            var mainVm = new MainWindowViewModel();
            var levelWithSnatcher = mainVm.LevelLayout.Levels[0];
            levelWithSnatcher.SnatcherBoxes = true;

            var levelWithoutSnatcher = EditorDefaultsService.CreateDefaultLevel(2, "Level 2 Without Snatchers");
            levelWithoutSnatcher.SnatcherBoxes = false;
            mainVm.LevelLayout.Levels.Add(levelWithoutSnatcher);

            var loader = new RundownLoader();
            loader.SaveProject(mainVm, tempFile);

            var loadedProject = loader.LoadProject(tempFile);
            var loadedVm = new MainWindowViewModel();
            loader.ApplyProject(loadedVm, loadedProject);

            var loaded1 = loadedVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == levelWithSnatcher.PersistentId);
            var loaded2 = loadedVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == 2);

            Assert.NotNull(loaded1);
            Assert.True(loaded1.SnatcherBoxes);

            Assert.NotNull(loaded2);
            Assert.False(loaded2.SnatcherBoxes);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void RundownDataBlockExporter_SnatcherBoxes_ExportsArenaDimensionBlock()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"gtfo_snatcher_export_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var mainVm = new MainWindowViewModel();
            var level = mainVm.LevelLayout.Levels[0];
            level.PersistentId = 42;
            level.SnatcherBoxes = true;

            var tierA = mainVm.Rundown.SelectedRundown?.TierA;
            Assert.NotNull(tierA);
            tierA.Clear();
            var exp = new ExpeditionBlock
            {
                Enabled = true,
                LevelLayoutData = 42
            };
            tierA.Add(exp);

            var exportResult = DatablockManager.Instance.ExportDatablocks(mainVm, tempDir);
            Assert.True(exportResult.Success, exportResult.Message);

            string rundownFilePath = Path.Combine(tempDir, "GameData_RundownDataBlock_bin.json");
            Assert.True(File.Exists(rundownFilePath));

            string json = File.ReadAllText(rundownFilePath);
            var rundownDoc = JsonNode.Parse(json) as JsonObject;
            var blocks = rundownDoc?["Blocks"] as JsonArray;
            Assert.NotNull(blocks);

            var firstBlock = blocks[0] as JsonObject;
            var tierAObj = firstBlock?["TierA"] as JsonArray;
            Assert.NotNull(tierAObj);

            var expObj = tierAObj[0] as JsonObject;
            Assert.NotNull(expObj);

            var dimDatas = expObj["DimensionDatas"] as JsonArray;
            Assert.NotNull(dimDatas);

            var arenaDim = dimDatas.FirstOrDefault(d => string.Equals(d?["DimensionIndex"]?.ToString(), "ARENA_DIMENSION", StringComparison.OrdinalIgnoreCase)) as JsonObject;
            Assert.NotNull(arenaDim);
            Assert.Equal(14u, arenaDim["DimensionData"]?.GetValue<uint>());
            Assert.True(arenaDim["Enabled"]?.GetValue<bool>());

            // Check DimensionDataBlock always has persistentID 14
            string dimFilePath = Path.Combine(tempDir, "GameData_DimensionDataBlock_bin.json");
            Assert.True(File.Exists(dimFilePath));

            string dimJson = File.ReadAllText(dimFilePath);
            var dimDoc = JsonNode.Parse(dimJson) as JsonObject;
            var dimBlocks = dimDoc?["Blocks"] as JsonArray;
            Assert.NotNull(dimBlocks);

            var block14 = dimBlocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 14);
            Assert.NotNull(block14);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void LevelLayoutDataBlockImporter_SnatcherBoxes_ParsesArenaDimensionFromExpedition()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"gtfo_snatcher_import_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var mainVm = new MainWindowViewModel();
            var level = mainVm.LevelLayout.Levels[0];
            level.PersistentId = 88;
            level.SnatcherBoxes = true;

            var tierA = mainVm.Rundown.SelectedRundown?.TierA;
            Assert.NotNull(tierA);
            tierA.Clear();
            var exp = new ExpeditionBlock
            {
                Enabled = true,
                LevelLayoutData = 88
            };
            tierA.Add(exp);

            var exportResult = DatablockManager.Instance.ExportDatablocks(mainVm, tempDir);
            Assert.True(exportResult.Success, exportResult.Message);

            // Now import back
            var importVm = new MainWindowViewModel();
            var importResult = DatablockManager.Instance.ImportDatablocks(importVm, tempDir);
            Assert.True(importResult.Success, importResult.Message);

            var importedLevel = importVm.LevelLayout.Levels.FirstOrDefault(l => l.PersistentId == 88);
            Assert.NotNull(importedLevel);
            Assert.True(importedLevel.SnatcherBoxes);
            // Ensure ARENA_DIMENSION is NOT in Dimensions list
            Assert.DoesNotContain(importedLevel.Dimensions, d => string.Equals(d.DimensionIndex, "ARENA_DIMENSION", StringComparison.OrdinalIgnoreCase));
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
