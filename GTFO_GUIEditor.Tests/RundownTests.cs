using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class RundownTests
{
    [Fact]
    public void ColorData_DefaultAndSetArgb_WorksCorrectly()
    {
        var color = new ColorData();
        Assert.Equal(1.0, color.A);
        Assert.Equal(1.0, color.R);
        Assert.Equal(1.0, color.G);
        Assert.Equal(1.0, color.B);
        Assert.Equal("#FFFFFFFF", color.HexCode);
        Assert.Equal(255, color.AByte);
        Assert.Equal(255, color.RByte);
        Assert.Equal(255, color.GByte);
        Assert.Equal(255, color.BByte);

        color.SetArgb(1.0, 0.5, 0.25, 0.0);
        Assert.Equal(1.0, color.A);
        Assert.Equal(0.5, color.R);
        Assert.Equal(0.25, color.G);
        Assert.Equal(0.0, color.B);
        Assert.Equal(255, color.AByte);
        Assert.Equal(128, color.RByte);
        Assert.Equal(64, color.GByte);
        Assert.Equal(0, color.BByte);
        Assert.Equal("#FF804000", color.HexCode);
    }

    [Fact]
    public void ColorData_SetFromHex_Parses6And8DigitHex()
    {
        var color = new ColorData();

        bool res6 = color.SetFromHex("#FF8000");
        Assert.True(res6);
        Assert.Equal(1.0, color.A);
        Assert.Equal(255, color.RByte);
        Assert.Equal(128, color.GByte);
        Assert.Equal(0, color.BByte);

        bool res8 = color.SetFromHex("#8000FF80");
        Assert.True(res8);
        Assert.Equal(128, color.AByte);
        Assert.Equal(0, color.RByte);
        Assert.Equal(255, color.GByte);
        Assert.Equal(128, color.BByte);

        bool resWithoutHash = color.SetFromHex("00FFAA");
        Assert.True(resWithoutHash);
        Assert.Equal(0, color.RByte);
        Assert.Equal(255, color.GByte);
        Assert.Equal(170, color.BByte);

        bool resInvalid = color.SetFromHex("InvalidHex");
        Assert.False(resInvalid);
    }

    [Fact]
    public void ColorData_PropertyNotification_FiresWhenChanged()
    {
        var color = new ColorData(1.0, 0.0, 0.0, 0.0);
        var changedProps = new List<string>();
        color.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null) changedProps.Add(e.PropertyName);
        };

        color.R = 0.8;
        Assert.Contains(nameof(ColorData.R), changedProps);
        Assert.Contains(nameof(ColorData.RByte), changedProps);
        Assert.Contains(nameof(ColorData.HexCode), changedProps);

        changedProps.Clear();
        color.HexCode = "#00FF00";
        Assert.Contains(nameof(ColorData.HexCode), changedProps);
        Assert.Equal(255, color.GByte);
    }

    [Fact]
    public void ColorData_Clone_CreatesIndependentCopy()
    {
        var original = new ColorData(1.0, 0.7, 0.4, 0.2);
        var clone = original.Clone();

        Assert.Equal(original.A, clone.A);
        Assert.Equal(original.R, clone.R);
        Assert.Equal(original.G, clone.G);
        Assert.Equal(original.B, clone.B);

        clone.R = 0.1;
        Assert.NotEqual(original.R, clone.R);
    }

    [Fact]
    public void TierVisuals_CloneAndDefaults_WorkCorrectly()
    {
        var visuals = new TierVisuals
        {
            Color = new ColorData(1.0, 0.9, 0.8, 0.7),
            Scale = 0.8,
            ScaleYModifier = 0.4
        };

        Assert.Equal(0.8, visuals.Scale);
        Assert.Equal(0.4, visuals.ScaleYModifier);
        Assert.Equal(visuals.Color.HexCode, visuals.ColorHex);

        var clone = visuals.Clone();
        Assert.Equal(visuals.Scale, clone.Scale);
        Assert.Equal(visuals.ScaleYModifier, clone.ScaleYModifier);
        Assert.Equal(visuals.ColorHex, clone.ColorHex);

        clone.Scale = 1.2;
        Assert.NotEqual(visuals.Scale, clone.Scale);
    }

    [Fact]
    public void TierRequirements_CloneAndDisplayText_WorkCorrectly()
    {
        var req = new TierRequirements
        {
            MainSectors = 3,
            SecondarySectors = 2,
            ThirdSectors = 1,
            AllClearedSectors = 0
        };

        Assert.True(req.HasAnyRequirement);
        Assert.Equal("Main: 3, Sec: 2, Overload: 1, All: 0", req.DisplayText);

        var clone = req.Clone();
        Assert.Equal(req.MainSectors, clone.MainSectors);
        Assert.Equal(req.SecondarySectors, clone.SecondarySectors);
        Assert.Equal(req.ThirdSectors, clone.ThirdSectors);
        Assert.Equal(req.AllClearedSectors, clone.AllClearedSectors);
    }

    [Fact]
    public void RundownBlock_CloningAndDisplayText_WorkCorrectly()
    {
        var rundown = new RundownBlock
        {
            Name = "Sector Zeta",
            PersistentId = 99,
            InternalEnabled = true,
            NeverShowRundownTree = false,
            UseTierUnlockRequirements = true
        };

        rundown.TierA.Add(new ExpeditionBlock
        {
            Enabled = true,
            Descriptive = new ExpeditionDescriptiveData { Prefix = "A1", PublicName = "The Fall" }
        });

        Assert.Equal("Sector Zeta [99]", rundown.DisplayText);

        var clone = rundown.Clone();
        Assert.Equal(rundown.Name, clone.Name);
        Assert.Equal(rundown.PersistentId, clone.PersistentId);
        Assert.Equal(rundown.UseTierUnlockRequirements, clone.UseTierUnlockRequirements);
        Assert.Single(clone.TierA);
        Assert.Equal("The Fall", clone.TierA[0].Descriptive.PublicName);

        clone.Name = "Sector Omega";
        Assert.NotEqual(rundown.Name, clone.Name);
    }

    [Fact]
    public void RundownService_LoadsVanillaBlocks_Successfully()
    {
        var vanillaBlocks = RundownService.LoadVanillaBlocks();
        Assert.NotNull(vanillaBlocks);
        Assert.NotEmpty(vanillaBlocks);

        var r7 = vanillaBlocks.FirstOrDefault(r => r.PersistentId == 31);
        Assert.NotNull(r7);
        Assert.Equal("Rundown 7.0", r7.Name);
        Assert.NotEmpty(r7.TierA);
        Assert.NotNull(r7.StorytellingData?.Visuals?.TierAVisuals?.Color);
    }

    [Fact]
    public void RundownService_CreateFreshBlock_InitializesDefaults()
    {
        var fresh = RundownService.CreateFreshBlock(101, "Custom Rundown 101");
        Assert.Equal(101u, fresh.PersistentId);
        Assert.Equal("Custom Rundown 101", fresh.Name);
        Assert.NotNull(fresh.StorytellingData?.Visuals?.TierAVisuals);
        Assert.Empty(fresh.TierA);
        Assert.Empty(fresh.TierB);
        Assert.Empty(fresh.TierC);
        Assert.Empty(fresh.TierD);
        Assert.Empty(fresh.TierE);
    }

    [Fact]
    public void RundownService_CreateBlockFromTemplate_ClonesWithNewIdAndName()
    {
        var vanillaBlocks = RundownService.LoadVanillaBlocks();
        var template = vanillaBlocks.First();

        var customized = RundownService.CreateBlockFromTemplate(template, 555, "Cloned Rundown");
        Assert.Equal(555u, customized.PersistentId);
        Assert.Equal("Cloned Rundown", customized.Name);
        Assert.Equal(template.TierA.Count, customized.TierA.Count);
    }

    [Fact]
    public void RundownTierViewModel_AddAndRemoveExpedition_SyncsWithRundown()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var vm = new RundownTierViewModel(
            rundown,
            "A",
            rundown.StorytellingData.Visuals.TierAVisuals,
            rundown.ReqToReachTierA,
            rundown.TierA);

        Assert.False(vm.HasExpeditions);
        Assert.Equal(0, vm.ExpeditionCount);

        vm.AddExpedition();
        Assert.True(vm.HasExpeditions);
        Assert.Equal(1, vm.ExpeditionCount);
        Assert.Equal("A", vm.Expeditions[0].Descriptive.Prefix);
        Assert.Equal("A1", vm.Expeditions[0].DisplayPrefix);
        Assert.Single(rundown.TierA);

        var exp = vm.Expeditions[0];
        vm.RemoveExpedition(exp);
        Assert.False(vm.HasExpeditions);
        Assert.Equal(0, vm.ExpeditionCount);
        Assert.Empty(rundown.TierA);
    }

    [Fact]
    public void RundownTierViewModel_VisualModifications_UpdateLiveProperties()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var tierVm = new RundownTierViewModel(
            rundown,
            "B",
            rundown.StorytellingData.Visuals.TierBVisuals,
            rundown.ReqToReachTierB,
            rundown.TierB);

        tierVm.Scale = 1.5;
        Assert.Equal(1.5, rundown.StorytellingData.Visuals.TierBVisuals.Scale);
        Assert.Equal(180.0, tierVm.ScaledWidth);
        Assert.Equal(100.0, tierVm.ScaledHeight);
        Assert.True(tierVm.ExpeditionSpacing > 100.0);
        Assert.Equal(24.0, tierVm.TierVerticalMargin);
        Assert.Equal(24.0, tierVm.TierMargin.Bottom);

        tierVm.ScaleYModifier = 0.8;
        Assert.Equal(0.8, rundown.StorytellingData.Visuals.TierBVisuals.ScaleYModifier);
        Assert.Equal(180.0, tierVm.ScaledWidth);
        Assert.Equal(100.0, tierVm.ScaledHeight);
        Assert.Equal(24.0, tierVm.TierVerticalMargin);
        Assert.Equal(24.0, tierVm.TierMargin.Bottom);

        tierVm.TierColor.SetFromHex("#FF112233");
        Assert.Equal("#FF112233", tierVm.ColorHex);
    }

    [Fact]
    public void RundownTierViewModel_ScaleChanges_OnlyAffectExpeditionSpacingAndDoNotAffectVerticalMargins()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var tierVm = new RundownTierViewModel(
            rundown,
            "A",
            rundown.StorytellingData.Visuals.TierAVisuals,
            rundown.ReqToReachTierA,
            rundown.TierA);

        double initialVerticalMargin = tierVm.TierVerticalMargin;
        Thickness initialMargin = tierVm.TierMargin;
        double initialSpacing = tierVm.ExpeditionSpacing;

        tierVm.Scale = 2.5;

        // Scale should affect horizontal spacing
        Assert.True(tierVm.ExpeditionSpacing > initialSpacing);

        // Scale should NOT affect vertical margins, tier dimensions or move other tiers
        Assert.Equal(initialVerticalMargin, tierVm.TierVerticalMargin);
        Assert.Equal(initialMargin, tierVm.TierMargin);
        Assert.Equal(180.0, tierVm.ScaledWidth);
        Assert.Equal(100.0, tierVm.ScaledHeight);

        // ScaleYModifier should also not affect vertical tier margins
        tierVm.ScaleYModifier = 1.8;
        Assert.Equal(initialVerticalMargin, tierVm.TierVerticalMargin);
        Assert.Equal(initialMargin, tierVm.TierMargin);
    }

    [Fact]
    public void RundownViewModel_InitializesAndRebuildsTiers()
    {
        var vanillaBlocks = RundownService.LoadVanillaBlocks();
        var vm = new RundownViewModel(vanillaBlocks);

        Assert.NotNull(vm.SelectedRundown);
        Assert.True(vm.HasSelectedRundown);
        Assert.Equal(5, vm.Tiers.Count);
        Assert.Equal("Tier A", vm.Tiers[0].TierName);
        Assert.Equal("Tier E", vm.Tiers[4].TierName);
        Assert.False(string.IsNullOrEmpty(vm.BackgroundColorHex));
    }

    [Fact]
    public void RundownViewModel_SingleRundown_InitializesFromVariousConstructors()
    {
        // 1. Parameterless constructor
        var vmDefault = new RundownViewModel();
        Assert.NotNull(vmDefault.SelectedRundown);
        Assert.True(vmDefault.HasSelectedRundown);
        Assert.Equal(5, vmDefault.Tiers.Count);

        // 2. Single RundownBlock constructor
        var customBlock = RundownService.CreateFreshBlock(100, "Custom Single Rundown");
        var vmSingle = new RundownViewModel(customBlock);
        Assert.Equal(customBlock, vmSingle.SelectedRundown);
        Assert.Equal(100u, vmSingle.SelectedRundown?.PersistentId);
        Assert.Equal("Custom Single Rundown", vmSingle.SelectedRundown?.Name);
        Assert.Equal(5, vmSingle.Tiers.Count);

        // 3. IEnumerable<RundownBlock> constructor uses the first rundown
        var r1 = RundownService.CreateFreshBlock(1, "First Rundown");
        var r2 = RundownService.CreateFreshBlock(2, "Second Rundown");
        var vmList = new RundownViewModel(new List<RundownBlock> { r1, r2 });
        Assert.Equal(r1, vmList.SelectedRundown);
        Assert.Equal("First Rundown", vmList.SelectedRundown?.Name);
    }

    [Fact]
    public void RundownViewModel_SingleRundown_UpdatesBackgroundAndSettings()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var vm = new RundownViewModel(rundown);

        Assert.False(vm.IsEditingRundownSettings);
        vm.OpenRundownSettings();
        Assert.True(vm.IsEditingRundownSettings);

        vm.SetBackgroundColor("#FF123456");
        Assert.Equal("#FF123456", vm.BackgroundColorHex);

        vm.CloseRundownSettings();
        Assert.False(vm.IsEditingRundownSettings);
    }

    [Fact]
    public void RundownViewModel_TierSettingsAndExpeditionModals_OpenAndCloseCorrectly()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var vm = new RundownViewModel(new List<RundownBlock> { rundown });

        var tierA = vm.Tiers[0];
        vm.OpenTierSettings(tierA);
        Assert.True(vm.IsEditingTierSettings);
        Assert.Equal(tierA, vm.ActiveEditingTier);

        vm.ApplyColorPreset("#FF00D2FF");
        Assert.Equal("#FF00D2FF", tierA.ColorHex);

        vm.CloseTierSettings();
        Assert.False(vm.IsEditingTierSettings);
        Assert.Null(vm.ActiveEditingTier);

        tierA.AddExpedition();
        var exp = tierA.Expeditions[0];
        vm.OpenExpeditionEditor(exp, tierA);
        Assert.True(vm.IsEditingExpedition);
        Assert.Equal(exp, vm.ActiveEditingExpedition);

        vm.CloseExpeditionEditor();
        Assert.False(vm.IsEditingExpedition);
        Assert.Null(vm.ActiveEditingExpedition);
    }

    [Fact]
    public void RundownTierViewModel_DuplicateExpedition_ClonesAndAppends()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var tierVm = new RundownTierViewModel(
            rundown,
            "C",
            rundown.StorytellingData.Visuals.TierCVisuals,
            rundown.ReqToReachTierC,
            rundown.TierC);

        tierVm.AddExpedition();
        var orig = tierVm.Expeditions[0];
        orig.Descriptive.PublicName = "Reactor Alpha";
        orig.SecondaryLayerEnabled = true;

        var copy = tierVm.DuplicateExpedition(orig);
        Assert.Equal(2, tierVm.Expeditions.Count);
        Assert.Equal("C2", copy.Descriptive.Prefix);
        Assert.Equal("Reactor Alpha (Copy)", copy.Descriptive.PublicName);
        Assert.True(copy.SecondaryLayerEnabled);
    }

    [Fact]
    public void ExpeditionBlock_Clone_CreatesDeepIndependentCopy()
    {
        var exp = new ExpeditionBlock
        {
            Enabled = true,
            Accessibility = ExpeditionAccessibility.Normal,
            LevelLayoutData = 12345,
            SecondaryLayerEnabled = true,
            ThirdLayerEnabled = true,
            Descriptive = new ExpeditionDescriptiveData
            {
                Prefix = "R7D1",
                PublicName = "Kowloon",
                ExpeditionDepth = 850
            },
            MainLayerData = new ExpeditionLayerData
            {
                ObjectiveData = new ExpeditionObjectiveData { WinCondition = "GoToExitGeo", DataBlockId = 44 }
            },
            Expedition = new ExpeditionSettings
            {
                ComplexResourceData = 12,
                DustColor = new ColorData(1.0, 0.2, 0.4, 0.6)
            }
        };

        var clone = exp.Clone();
        Assert.Equal(exp.LevelLayoutData, clone.LevelLayoutData);
        Assert.Equal(exp.Descriptive.Prefix, clone.Descriptive.Prefix);
        Assert.Equal(exp.Descriptive.PublicName, clone.Descriptive.PublicName);
        Assert.Equal("GoToExitGeo", clone.MainLayerData.ObjectiveData.WinCondition);
        Assert.Equal(12u, clone.Expedition.ComplexResourceData);
        Assert.Equal(0.2, clone.Expedition.DustColor.R);

        clone.Descriptive.Prefix = "R7D2";
        Assert.NotEqual(exp.Descriptive.Prefix, clone.Descriptive.Prefix);
    }

    [Fact]
    public void RundownTierViewModel_DisabledExpeditions_AffectVisibilityAndCounts()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var tierVm = new RundownTierViewModel(
            rundown,
            "A",
            rundown.StorytellingData.Visuals.TierAVisuals,
            rundown.ReqToReachTierA,
            rundown.TierA);

        Assert.False(tierVm.HasExpeditions);
        Assert.False(tierVm.HasVisibleExpeditions);
        Assert.Equal(0, tierVm.VisibleExpeditionCount);
        Assert.Equal("0 Expeditions", tierVm.ExpeditionCountText);

        tierVm.AddExpedition();
        Assert.True(tierVm.HasExpeditions);
        Assert.True(tierVm.HasVisibleExpeditions);
        Assert.Equal(1, tierVm.VisibleExpeditionCount);
        Assert.Equal("1 Expedition", tierVm.ExpeditionCountText);

        var exp1 = tierVm.Expeditions[0];
        tierVm.AddExpedition();
        var exp2 = tierVm.Expeditions[1];
        Assert.Equal(2, tierVm.VisibleExpeditionCount);
        Assert.Equal("2 Expeditions", tierVm.ExpeditionCountText);

        // Disable exp2
        exp2.Enabled = false;
        Assert.Equal(2, tierVm.ExpeditionCount);
        Assert.Equal(1, tierVm.VisibleExpeditionCount);
        Assert.True(tierVm.HasVisibleExpeditions);
        Assert.Equal("1 Expedition", tierVm.ExpeditionCountText);

        // Disable exp1
        exp1.Enabled = false;
        Assert.Equal(2, tierVm.ExpeditionCount);
        Assert.Equal(0, tierVm.VisibleExpeditionCount);
        Assert.False(tierVm.HasVisibleExpeditions);
        Assert.Equal("0 Expeditions", tierVm.ExpeditionCountText);

        // Re-enable exp1
        exp1.Enabled = true;
        Assert.Equal(1, tierVm.VisibleExpeditionCount);
        Assert.True(tierVm.HasVisibleExpeditions);
        Assert.Equal("1 Expedition", tierVm.ExpeditionCountText);
    }

    [Fact]
    public void RundownTierViewModel_OpenExpedition_TriggersEditorCallback()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var vm = new RundownViewModel(new List<RundownBlock> { rundown });
        var tierA = vm.Tiers[0];

        tierA.AddExpedition();
        var exp = tierA.Expeditions[0];

        Assert.False(vm.IsEditingExpedition);
        Assert.Null(vm.ActiveEditingExpedition);

        // Calling OpenExpedition directly on the tier ViewModel (as bound from button)
        tierA.OpenExpedition(exp);

        Assert.True(vm.IsEditingExpedition);
        Assert.Equal(exp, vm.ActiveEditingExpedition);
        Assert.Equal(tierA, vm.ActiveEditingExpeditionTier);
    }

    [Fact]
    public void ExpeditionBlock_SkipExpNumberInName_ControlsDisplayPrefixIncrementing()
    {
        var exp1 = new ExpeditionBlock
        {
            ExpeditionIndexInTier = 1,
            Descriptive = new ExpeditionDescriptiveData
            {
                Prefix = "R7B",
                PublicName = "Cargo",
                SkipExpNumberInName = false
            }
        };

        var exp2 = new ExpeditionBlock
        {
            ExpeditionIndexInTier = 2,
            Descriptive = new ExpeditionDescriptiveData
            {
                Prefix = "R7B",
                PublicName = "Dense",
                SkipExpNumberInName = false
            }
        };

        // When SkipExpNumberInName is false: incrementing number appended
        Assert.Equal("R7B1", exp1.DisplayPrefix);
        Assert.Equal("R7B2", exp2.DisplayPrefix);

        // When SkipExpNumberInName is true: no number appended
        exp1.Descriptive.SkipExpNumberInName = true;
        exp2.Descriptive.SkipExpNumberInName = true;
        Assert.Equal("R7B", exp1.DisplayPrefix);
        Assert.Equal("R7B", exp2.DisplayPrefix);
    }

    [Fact]
    public void ExpeditionBlock_StoryVisualStyle_ProvidesGoldBorderOutline()
    {
        var exp = new ExpeditionBlock
        {
            TierColorHex = "#FF123456",
            Descriptive = new ExpeditionDescriptiveData
            {
                Prefix = "R7C",
                PublicName = "Depot",
                ProgressionVisualStyle = "Normal"
            }
        };

        // Normal level outline uses grey #303030
        Assert.False(exp.IsStory);
        Assert.Equal("#303030", exp.BorderBrushHex);

        // Story level outline uses gold
        exp.Descriptive.ProgressionVisualStyle = "Story";
        Assert.True(exp.IsStory);
        Assert.Equal("#E5A93C", exp.BorderBrushHex);

        // Story style property variant
        exp.Descriptive.ProgressionVisualStyle = "Normal";
        exp.HasStoryStyle = true;
        Assert.True(exp.IsStory);
        Assert.Equal("#E5A93C", exp.BorderBrushHex);
    }

    [Fact]
    public void RundownTierViewModel_TierExpeditions_AutomaticallyNumberedSequentially()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var tierVm = new RundownTierViewModel(
            rundown,
            "B",
            rundown.StorytellingData.Visuals.TierBVisuals,
            rundown.ReqToReachTierB,
            rundown.TierB);

        tierVm.AddExpedition();
        tierVm.AddExpedition();
        tierVm.AddExpedition();

        Assert.Equal(3, tierVm.Expeditions.Count);
        Assert.Equal("B1", tierVm.Expeditions[0].DisplayPrefix);
        Assert.Equal("B2", tierVm.Expeditions[1].DisplayPrefix);
        Assert.Equal("B3", tierVm.Expeditions[2].DisplayPrefix);

        // Remove the second expedition (B2)
        tierVm.RemoveExpedition(tierVm.Expeditions[1]);

        Assert.Equal(2, tierVm.Expeditions.Count);
        Assert.Equal("B1", tierVm.Expeditions[0].DisplayPrefix);
        Assert.Equal("B2", tierVm.Expeditions[1].DisplayPrefix);
    }

    [Fact]
    public void SectorIconAssetFiles_ExistOnDisk()
    {
        string baseDir = AppContext.BaseDirectory;
        // Search upwards to find GTFO_GUIEditor/Assets folder
        string? projectAssetsDir = null;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "GTFO_GUIEditor", "Assets");
            if (Directory.Exists(candidate))
            {
                projectAssetsDir = candidate;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(projectAssetsDir);
        Assert.True(File.Exists(Path.Combine(projectAssetsDir, "Sector_Main.png")), "Sector_Main.png should exist in Assets");
        Assert.True(File.Exists(Path.Combine(projectAssetsDir, "Sector_Secondary.png")), "Sector_Secondary.png should exist in Assets");
        Assert.True(File.Exists(Path.Combine(projectAssetsDir, "Sector_Overload.png")), "Sector_Overload.png should exist in Assets");
    }
    [Fact]
    public void RundownViewModel_DeleteExpeditionConfirmationFlow_WorksCorrectly()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var vm = new RundownViewModel(new List<RundownBlock> { rundown });
        var tierA = vm.Tiers[0];

        tierA.AddExpedition();
        tierA.AddExpedition();
        var exp1 = tierA.Expeditions[0];
        var exp2 = tierA.Expeditions[1];

        // 1. Request deletion from tier button
        Assert.False(vm.IsConfirmingDeleteExpedition);
        Assert.Null(vm.ExpeditionToDelete);
        Assert.Null(vm.ExpeditionTierToDelete);

        tierA.RequestDeleteExpedition(exp1);

        Assert.True(vm.IsConfirmingDeleteExpedition);
        Assert.Equal(exp1, vm.ExpeditionToDelete);
        Assert.Equal(tierA, vm.ExpeditionTierToDelete);
        Assert.Contains(exp1.DisplayPrefix, vm.DeleteExpeditionConfirmationMessage);
        Assert.Contains("Level A1", vm.DeleteExpeditionConfirmationMessage);

        // 2. Cancel deletion
        vm.CancelDeleteExpedition();
        Assert.False(vm.IsConfirmingDeleteExpedition);
        Assert.Null(vm.ExpeditionToDelete);
        Assert.Null(vm.ExpeditionTierToDelete);
        Assert.Equal(2, tierA.Expeditions.Count);

        // 3. Open editor on exp1, then request delete exp1, then confirm delete
        tierA.OpenExpedition(exp1);
        Assert.True(vm.IsEditingExpedition);
        Assert.Equal(exp1, vm.ActiveEditingExpedition);

        tierA.RequestDeleteExpedition(exp1);
        Assert.True(vm.IsConfirmingDeleteExpedition);

        vm.ConfirmDeleteExpedition();
        Assert.False(vm.IsConfirmingDeleteExpedition);
        Assert.Null(vm.ExpeditionToDelete);
        Assert.Null(vm.ExpeditionTierToDelete);
        Assert.False(vm.IsEditingExpedition);
        Assert.Null(vm.ActiveEditingExpedition);

        // Remaining expedition re-indexed
        Assert.Single(tierA.Expeditions);
        Assert.Equal(exp2, tierA.Expeditions[0]);
        Assert.Equal("A1", tierA.Expeditions[0].DisplayPrefix);
    }

    [Fact]
    public void TextService_ResolvesEnglishText_Correctly()
    {
        // persistentID: 1727372250 -> "RUNDOWN #7.0 \nTITLE: RISE"
        // persistentID: 1026056473 -> "[STATUS] OBJECTIVE STACK REITERATION 7.0..."
        string title = TextService.GetEnglishText(1727372250);
        string intel = TextService.GetEnglishText(1026056473);

        Assert.Contains("RISE", title);
        Assert.Contains("OBJECTIVE STACK REITERATION 7.0", intel);
    }

    [Fact]
    public void StorytellingData_TranslatesNumericIDsToStringsOnDeserialization()
    {
        string json = """
        {
            "Title": 1727372250,
            "SurfaceDescription": 1026056473
        }
        """;

        var data = JsonSerializer.Deserialize<StorytellingData>(json);
        Assert.NotNull(data);
        Assert.Contains("RISE", data.Title);
        Assert.Contains("OBJECTIVE STACK REITERATION 7.0", data.SurfaceDescription);

        // Edit values as strings
        data.Title = "Edited Custom Title";
        data.SurfaceDescription = "Edited Intel text.";
        Assert.Equal("Edited Custom Title", data.Title);
        Assert.Equal("Edited Intel text.", data.SurfaceDescription);
    }

    [Fact]
    public void RundownService_LoadsVanillaBlocks_TranslatesStorytellingData()
    {
        var vanillaBlocks = RundownService.LoadVanillaBlocks();
        var r7 = vanillaBlocks.FirstOrDefault(r => r.PersistentId == 31);

        Assert.NotNull(r7);
        Assert.NotNull(r7.StorytellingData);
        Assert.Contains("RISE", r7.StorytellingData.Title);
        Assert.Contains("OBJECTIVE STACK REITERATION 7.0", r7.StorytellingData.SurfaceDescription);
    }

    [Fact]
    public void RundownViewModel_IntelDialog_OpensAndCloses()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        rundown.StorytellingData.Title = "My Title";
        rundown.StorytellingData.SurfaceDescription = "Surface Intel Content";

        var vm = new RundownViewModel(new List<RundownBlock> { rundown });

        Assert.False(vm.IsShowingIntel);
        vm.OpenIntelDialog();
        Assert.True(vm.IsShowingIntel);

        vm.CloseIntelDialog();
        Assert.False(vm.IsShowingIntel);
    }

    [Fact]
    public void ExpeditionAccessibility_EnumValues_MatchExpectedOptions()
    {
        var values = Enum.GetNames<ExpeditionAccessibility>();
        Assert.Equal(6, values.Length);
        Assert.Contains("Normal", values);
        Assert.Contains("AlwayBlock", values);
        Assert.Contains("AlwaysAllow", values);
        Assert.Contains("BlockedAndScrambled", values);
        Assert.Contains("UseCustomProgressionLock", values);
        Assert.Contains("UnlockedByExpedition", values);
    }

    [Fact]
    public void ExpeditionAccessibility_JsonSerializationAndDeserialization_WorksCorrectly()
    {
        var exp = new ExpeditionBlock
        {
            Accessibility = ExpeditionAccessibility.AlwayBlock
        };

        string json = JsonSerializer.Serialize(exp);
        Assert.Contains("\"Accessibility\":\"AlwayBlock\"", json);

        var deserialized = JsonSerializer.Deserialize<ExpeditionBlock>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(ExpeditionAccessibility.AlwayBlock, deserialized.Accessibility);

        // Test UnlockedByExpedition
        exp.Accessibility = ExpeditionAccessibility.UnlockedByExpedition;
        json = JsonSerializer.Serialize(exp);
        Assert.Contains("\"Accessibility\":\"UnlockedByExpedition\"", json);

        deserialized = JsonSerializer.Deserialize<ExpeditionBlock>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(ExpeditionAccessibility.UnlockedByExpedition, deserialized.Accessibility);
        Assert.True(deserialized.IsUnlockedByExpedition);
    }

    [Fact]
    public void RundownViewModel_ExpeditionLevelLayoutSelection_BindsAndUpdatesCorrectly()
    {
        var layout1 = new LevelLayoutBlock { PersistentId = 101, Name = "Level Alpha" };
        var layout2 = new LevelLayoutBlock { PersistentId = 102, Name = "Level Beta" };
        var levelLayouts = new ObservableCollection<LevelLayoutBlock> { layout1, layout2 };

        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var vm = new RundownViewModel(new List<RundownBlock> { rundown }, levelLayouts: levelLayouts);
        var tierA = vm.Tiers[0];
        tierA.AddExpedition();
        var exp = tierA.Expeditions[0];

        vm.OpenExpeditionEditor(exp, tierA);
        Assert.Null(vm.SelectedEditingExpeditionLevel);

        vm.SelectedEditingExpeditionLevel = layout2;
        Assert.Equal(102u, exp.LevelLayoutData);
        Assert.Equal(layout2, vm.SelectedEditingExpeditionLevel);

        exp.LevelLayoutData = 101;
        Assert.Equal(layout1, vm.SelectedEditingExpeditionLevel);
    }

    [Fact]
    public void RundownViewModel_UnlockedByExpedition_OtherLevelsSelection_WorksCorrectly()
    {
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var vm = new RundownViewModel(new List<RundownBlock> { rundown });
        var tierA = vm.Tiers[0];
        var tierB = vm.Tiers[1];

        tierA.AddExpedition(); // A1
        tierA.AddExpedition(); // A2
        tierB.AddExpedition(); // B1

        var expA1 = tierA.Expeditions[0];
        var expA2 = tierA.Expeditions[1];
        var expB1 = tierB.Expeditions[0];

        expA1.Descriptive.PublicName = "Entry";
        expA2.Descriptive.PublicName = "Depth";
        expB1.Descriptive.PublicName = "Reactor";

        // Open editor on A2
        vm.OpenExpeditionEditor(expA2, tierA);

        // AvailableOtherExpeditions should contain A1 and B1, but NOT A2
        Assert.Equal(2, vm.AvailableOtherExpeditions.Count);
        Assert.DoesNotContain(vm.AvailableOtherExpeditions, item => item.Expedition == expA2);
        Assert.Contains(vm.AvailableOtherExpeditions, item => item.Expedition == expA1);
        Assert.Contains(vm.AvailableOtherExpeditions, item => item.Expedition == expB1);

        var a1Lookup = vm.AvailableOtherExpeditions.First(item => item.Expedition == expA1);
        Assert.Equal("TierA", a1Lookup.Tier);
        Assert.Equal("Exp1", a1Lookup.Exp);
        Assert.Equal("A1: Entry", a1Lookup.DisplayText);

        var b1Lookup = vm.AvailableOtherExpeditions.First(item => item.Expedition == expB1);
        Assert.Equal("TierB", b1Lookup.Tier);
        Assert.Equal("Exp1", b1Lookup.Exp);
        Assert.Equal("B1: Reactor", b1Lookup.DisplayText);

        // Change accessibility to UnlockedByExpedition and select B1 as prerequisite
        expA2.Accessibility = ExpeditionAccessibility.UnlockedByExpedition;
        Assert.True(vm.IsUnlockedByExpeditionSelected);

        vm.SelectedOtherExpedition = b1Lookup;
        Assert.Equal("TierB", expA2.UnlockedByExpedition.Tier);
        Assert.Equal("Exp1", expA2.UnlockedByExpedition.Exp);

        // Close and reopen A2
        vm.CloseExpeditionEditor();
        vm.OpenExpeditionEditor(expA2, tierA);

        Assert.NotNull(vm.SelectedOtherExpedition);
        Assert.Equal("TierB", vm.SelectedOtherExpedition.Tier);
        Assert.Equal("Exp1", vm.SelectedOtherExpedition.Exp);
        Assert.Equal(expB1, vm.SelectedOtherExpedition.Expedition);
    }

    [Fact]
    public void ExpeditionBlock_DefaultValues_HaveExpectedDefaults()
    {
        var exp = new ExpeditionBlock();
        Assert.False(exp.IsSinglePlayer);
        Assert.False(exp.SkipLobby);
        Assert.False(exp.PutIconAboveTier);
        Assert.False(exp.DisablePlayerVoicelines);
        Assert.False(exp.ExcludeFromProgression);
        Assert.False(exp.HideOnLocked);
        Assert.False(exp.HideRolePlayDataOnLocked);
    }

    [Fact]
    public void ExpeditionBlock_NewOptions_SerializationAndCloning()
    {
        var exp = new ExpeditionBlock
        {
            IsSinglePlayer = true,
            SkipLobby = true,
            PutIconAboveTier = true,
            DisablePlayerVoicelines = true,
            ExcludeFromProgression = true,
            HideOnLocked = true,
            HideRolePlayDataOnLocked = true
        };

        var clone = exp.Clone();
        Assert.True(clone.IsSinglePlayer);
        Assert.True(clone.SkipLobby);
        Assert.True(clone.PutIconAboveTier);
        Assert.True(clone.DisablePlayerVoicelines);
        Assert.True(clone.ExcludeFromProgression);
        Assert.True(clone.HideOnLocked);
        Assert.True(clone.HideRolePlayDataOnLocked);

        string json = JsonSerializer.Serialize(exp);
        Assert.Contains("\"IsSinglePlayer\":true", json);
        Assert.Contains("\"SkipLobby\":true", json);
        Assert.Contains("\"PutIconAboveTier\":true", json);
        Assert.Contains("\"DisablePlayerVoicelines\":true", json);
        Assert.Contains("\"ExcludeFromProgression\":true", json);
        Assert.Contains("\"HideOnLocked\":true", json);
        Assert.Contains("\"HideRolePlayDataOnLocked\":true", json);

        var deserialized = JsonSerializer.Deserialize<ExpeditionBlock>(json);
        Assert.NotNull(deserialized);
        Assert.True(deserialized.IsSinglePlayer);
        Assert.True(deserialized.SkipLobby);
        Assert.True(deserialized.PutIconAboveTier);
        Assert.True(deserialized.DisablePlayerVoicelines);
        Assert.True(deserialized.ExcludeFromProgression);
        Assert.True(deserialized.HideOnLocked);
        Assert.True(deserialized.HideRolePlayDataOnLocked);
    }

    [Fact]
    public void ExpeditionDescriptiveData_ExpeditionDescriptionAndWardenIntel_TranslatesTextDataBlocksAndCanBeEdited()
    {
        string json = """
        {
            "Prefix": "R8A1",
            "PublicName": "Order",
            "ExpeditionDescription": 3190211359,
            "RoleplayedWardenIntel": 331885931
        }
        """;

        var descriptive = JsonSerializer.Deserialize<ExpeditionDescriptiveData>(json);
        Assert.NotNull(descriptive);
        Assert.Equal("R8A1", descriptive.Prefix);
        Assert.Equal("Order", descriptive.PublicName);
        Assert.NotEmpty(descriptive.ExpeditionDescription);
        Assert.NotEmpty(descriptive.RoleplayedWardenIntel);

        // Edit text values
        descriptive.ExpeditionDescription = "Custom level description text";
        descriptive.RoleplayedWardenIntel = "Custom warden intel notes";

        var clone = descriptive.Clone();
        Assert.Equal("Custom level description text", clone.ExpeditionDescription);
        Assert.Equal("Custom warden intel notes", clone.RoleplayedWardenIntel);

        string serialized = JsonSerializer.Serialize(descriptive);
        Assert.Contains("Custom level description text", serialized);
        Assert.Contains("Custom warden intel notes", serialized);
    }

    [Fact]
    public void ExpeditionDescriptiveData_SupportsExpeditionDescriptiveAlias()
    {
        string json = """
        {
            "Prefix": "R8A1",
            "PublicName": "Order",
            "ExpeditionDescriptive": 3190211359,
            "RoleplayedWardenIntel": 331885931
        }
        """;

        var descriptive = JsonSerializer.Deserialize<ExpeditionDescriptiveData>(json);
        Assert.NotNull(descriptive);
        Assert.NotEmpty(descriptive.ExpeditionDescription);
    }

    [Fact]
    public void RundownViewModel_ExpeditionSectors_SyncWithLevelLayoutDoors()
    {
        var level1 = new LevelLayoutBlock
        {
            Name = "Level One",
            PersistentId = 100,
            ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab"
        };
        var level2 = new LevelLayoutBlock
        {
            Name = "Level Two",
            PersistentId = 200,
            ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab"
        };

        var levels = new ObservableCollection<LevelLayoutBlock> { level1, level2 };
        var rundown = RundownService.CreateFreshBlock(1, "Test Rundown");
        var vm = new RundownViewModel(new List<RundownBlock> { rundown }, levels);

        var tierA = vm.Tiers[0];
        tierA.AddExpedition();
        var expA1 = tierA.Expeditions[0];

        // Link expA1 to level1
        expA1.LevelLayoutData = 100;
        vm.SyncExpeditionSectorsWithLevel(expA1);

        // Initially level1 has no secondary or overload doors
        Assert.False(expA1.SecondaryLayerEnabled);
        Assert.False(expA1.ThirdLayerEnabled);

        // Add Secondary Door to level1
        var eastDoor = level1.GridState.PlacedDoors.First(d => d.Direction == "Right");
        var digSiteGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        level1.GridState.ExpandFromDoor(eastDoor, digSiteGeo, DoorType.SecondaryDoor);

        Assert.True(level1.HasSecondaryDoor);
        Assert.True(expA1.SecondaryLayerEnabled);
        Assert.False(expA1.ThirdLayerEnabled);

        // Add Overload Door to level1
        var southDoor = level1.GridState.PlacedDoors.First(d => d.Direction == "Backwards" && !d.IsConnected);
        level1.GridState.ExpandFromDoor(southDoor, digSiteGeo, DoorType.OverloadDoor);

        Assert.True(level1.HasOverloadDoor);
        Assert.True(expA1.SecondaryLayerEnabled);
        Assert.True(expA1.ThirdLayerEnabled);

        // Open expedition editor on expA1 -> sectors are synced
        vm.OpenExpeditionEditor(expA1, tierA);
        Assert.True(vm.ActiveEditingExpedition?.SecondaryLayerEnabled);
        Assert.True(vm.ActiveEditingExpedition?.ThirdLayerEnabled);

        // Switch to level2 (which has no secondary/overload doors)
        vm.SelectedEditingExpeditionLevel = level2;
        Assert.Equal(200u, expA1.LevelLayoutData);
        Assert.False(expA1.SecondaryLayerEnabled);
        Assert.False(expA1.ThirdLayerEnabled);
    }
}
