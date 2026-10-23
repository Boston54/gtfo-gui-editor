using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class LightingTests
{
    [Fact]
    public void LoadVanillaBlocks_LoadsBlocksSuccessfully()
    {
        var blocks = LightSettingsService.LoadVanillaBlocks();
        Assert.NotNull(blocks);
        Assert.NotEmpty(blocks);

        var firstBlock = blocks.FirstOrDefault(b => b.PersistentId == 1);
        Assert.NotNull(firstBlock);
        Assert.Equal("BlueToOrange_1", firstBlock.Name);
        Assert.True(firstBlock.InternalEnabled);
        Assert.Equal(8, firstBlock.LightCategorySettings.Count);

        var generalSetting = firstBlock.LightCategorySettings.FirstOrDefault(s => s.Category == LightCategory.General);
        Assert.NotNull(generalSetting);
        Assert.True(generalSetting.On);
        Assert.Equal(0.5, generalSetting.Chance);
        Assert.NotNull(generalSetting.Color);
        Assert.True(generalSetting.Color.R > 0);
    }

    [Fact]
    public void LoadVanillaBlocks_ParsesAllCategoriesCorrectly()
    {
        var blocks = LightSettingsService.LoadVanillaBlocks();
        var allCategories = blocks.SelectMany(b => b.LightCategorySettings).Select(s => s.Category).Distinct().ToList();

        Assert.Contains(LightCategory.General, allCategories);
        Assert.Contains(LightCategory.Special, allCategories);
        Assert.Contains(LightCategory.Emergency, allCategories);
        Assert.Contains(LightCategory.Independent, allCategories);
        Assert.Contains(LightCategory.Door, allCategories);
        Assert.Contains(LightCategory.Sign, allCategories);
        Assert.Contains(LightCategory.DoorImportant, allCategories);
    }

    [Fact]
    public void CreateFreshBlock_InitializesDefaults()
    {
        var fresh = LightSettingsService.CreateFreshBlock(999, "Custom_Lighting_Test");
        Assert.Equal((uint)999, fresh.PersistentId);
        Assert.Equal("Custom_Lighting_Test", fresh.Name);
        Assert.True(fresh.InternalEnabled);
        Assert.NotEmpty(fresh.LightCategorySettings);

        var general = fresh.LightCategorySettings.FirstOrDefault(s => s.Category == LightCategory.General);
        Assert.NotNull(general);
        Assert.True(general.On);
        Assert.Equal(0.5, general.Chance);
    }

    [Fact]
    public void CreateBlockFromTemplate_ClonesDeeply()
    {
        var blocks = LightSettingsService.LoadVanillaBlocks();
        var template = blocks.First(b => b.PersistentId == 1);

        var cloned = LightSettingsService.CreateBlockFromTemplate(template, 2005, "Cloned_Lighting_Config");

        Assert.Equal((uint)2005, cloned.PersistentId);
        Assert.Equal("Cloned_Lighting_Config", cloned.Name);
        Assert.Equal(template.LightCategorySettings.Count, cloned.LightCategorySettings.Count);

        // Modifying clone does not alter template
        cloned.LightCategorySettings.Add(new LightCategorySettingItem
        {
            Category = LightCategory.Emergency,
            On = true,
            Chance = 0.5
        });

        Assert.NotEqual(template.LightCategorySettings.Count, cloned.LightCategorySettings.Count);

        cloned.LightCategorySettings[0].Chance = 0.999;
        Assert.NotEqual(template.LightCategorySettings[0].Chance, cloned.LightCategorySettings[0].Chance);
    }

    [Fact]
    public void LightingViewModel_InitializesEmptyByDefault()
    {
        var vm = new LightingViewModel();
        Assert.Empty(vm.Blocks);
        Assert.Null(vm.SelectedBlock);
        Assert.False(vm.HasSelectedBlock);
        Assert.Contains("0 configs loaded", vm.StatusText);
    }

    [Fact]
    public void LightingViewModel_InitializesAndSwapsConfigs()
    {
        var vm = new LightingViewModel(LightSettingsService.LoadVanillaBlocks());
        Assert.NotEmpty(vm.Blocks);
        Assert.NotNull(vm.SelectedBlock);
        Assert.True(vm.HasSelectedBlock);
        Assert.Contains("configs loaded", vm.StatusText);

        var targetBlock = vm.Blocks.FirstOrDefault(b => b.PersistentId == 82);
        Assert.NotNull(targetBlock);

        vm.SelectedBlock = targetBlock;
        Assert.Equal((uint)82, vm.SelectedBlock.PersistentId);
        Assert.Equal("Tutorial_StealthSpotlight", vm.SelectedBlock.Name);
        Assert.Contains("Tutorial_StealthSpotlight", vm.StatusText);
    }

    [Fact]
    public void LightingViewModel_AddAndRemoveOption_UpdatesSelectedBlock()
    {
        var vm = new LightingViewModel(LightSettingsService.LoadVanillaBlocks());
        Assert.NotNull(vm.SelectedBlock);
        int initialCount = vm.SelectedBlock.LightCategorySettings.Count;

        vm.AddOption();
        Assert.Equal(initialCount + 1, vm.SelectedBlock.LightCategorySettings.Count);

        var added = vm.SelectedBlock.LightCategorySettings.Last();
        Assert.Equal(LightCategory.General, added.Category);

        vm.RemoveOption(added);
        Assert.Equal(initialCount, vm.SelectedBlock.LightCategorySettings.Count);
    }

    [Fact]
    public void LightingViewModel_DetectsDuplicatePersistentId()
    {
        var vm = new LightingViewModel(LightSettingsService.LoadVanillaBlocks());
        Assert.NotNull(vm.SelectedBlock);
        Assert.False(vm.HasDuplicatePersistentId);

        uint originalId = vm.SelectedBlock.PersistentId;
        var otherBlock = vm.Blocks.First(b => b != vm.SelectedBlock);

        // Change ID to match another block
        vm.SelectedBlock.PersistentId = otherBlock.PersistentId;
        Assert.True(vm.HasDuplicatePersistentId);
        Assert.Contains("used by", vm.DuplicatePersistentIdWarning);

        // Revert
        vm.SelectedBlock.PersistentId = originalId;
        Assert.False(vm.HasDuplicatePersistentId);
    }

    [Fact]
    public void LightingViewModel_CreateBlock_ValidationAndFresh()
    {
        var vm = new LightingViewModel(LightSettingsService.LoadVanillaBlocks());
        vm.OpenCreateBlockDialog();
        Assert.True(vm.IsCreatingBlock);
        Assert.True(vm.IsCreatingFresh);

        // Empty Name Validation
        vm.NewBlockName = "";
        Assert.False(vm.ConfirmCreateBlock());
        Assert.True(vm.HasCreationErrorMessage);

        // Duplicate ID Validation
        vm.NewBlockName = "Valid_Name";
        vm.NewBlockPersistentId = vm.Blocks.First().PersistentId;
        Assert.False(vm.ConfirmCreateBlock());
        Assert.True(vm.HasCreationErrorMessage);

        // Successful Fresh Creation
        uint newId = 888888;
        vm.NewBlockPersistentId = newId;
        vm.NewBlockName = "New_Fresh_Lighting";
        Assert.True(vm.ConfirmCreateBlock());
        Assert.False(vm.IsCreatingBlock);

        Assert.NotNull(vm.SelectedBlock);
        Assert.Equal(newId, vm.SelectedBlock.PersistentId);
        Assert.Equal("New_Fresh_Lighting", vm.SelectedBlock.Name);
        Assert.Contains(vm.Blocks, b => b.PersistentId == newId);
    }

    [Fact]
    public void LightingViewModel_CreateBlock_FromTemplate()
    {
        var vm = new LightingViewModel(LightSettingsService.LoadVanillaBlocks());
        vm.OpenCreateBlockDialog();
        vm.IsCreatingFromTemplate = true;
        vm.SelectedTemplate = vm.AvailableTemplates.First(t => t.PersistentId == 1);
        vm.NewBlockPersistentId = 777777;
        vm.NewBlockName = "New_Templated_Lighting";

        Assert.True(vm.ConfirmCreateBlock());
        Assert.NotNull(vm.SelectedBlock);
        Assert.Equal((uint)777777, vm.SelectedBlock.PersistentId);
        Assert.Equal(8, vm.SelectedBlock.LightCategorySettings.Count);
    }

    [Fact]
    public void LightingViewModel_DeleteBlock_Workflow()
    {
        var vm = new LightingViewModel(LightSettingsService.LoadVanillaBlocks());
        int initialCount = vm.Blocks.Count;
        var toDelete = vm.Blocks.First();

        vm.RequestDeleteBlock(toDelete);
        Assert.True(vm.IsConfirmingDelete);
        Assert.Equal(toDelete, vm.BlockToDelete);
        Assert.Contains(toDelete.Name, vm.DeleteConfirmationMessage);

        // Cancel
        vm.CancelDeleteBlock();
        Assert.False(vm.IsConfirmingDelete);
        Assert.Null(vm.BlockToDelete);
        Assert.Equal(initialCount, vm.Blocks.Count);

        // Confirm
        vm.RequestDeleteBlock(toDelete);
        Assert.True(vm.ConfirmDeleteBlock());
        Assert.Equal(initialCount - 1, vm.Blocks.Count);
        Assert.DoesNotContain(vm.Blocks, b => b.PersistentId == toDelete.PersistentId);
    }

    [Fact]
    public void LightColor_HexColorComputation()
    {
        var color = new LightColor { R = 1.0, G = 0.5, B = 0.0, A = 0.0 };
        Assert.Equal("#FF8000", color.HexColor);
        Assert.Equal("#00FF8000", color.HexCode);

        color.R = 0.0;
        color.G = 1.0;
        color.B = 1.0;
        Assert.Equal("#00FFFF", color.HexColor);
        Assert.Equal("#0000FFFF", color.HexCode);
    }

    [Fact]
    public void LightColor_SetFromHex_Parses6And8DigitHex()
    {
        var color = new LightColor { R = 0.0, G = 0.0, B = 0.0, A = 0.0 };

        // 6-digit hex retains existing A
        bool parsed6 = color.SetFromHex("#FF8000");
        Assert.True(parsed6);
        Assert.Equal(1.0, color.R, 2);
        Assert.Equal(0.5, color.G, 2);
        Assert.Equal(0.0, color.B, 2);
        Assert.Equal(0.0, color.A, 2);
        Assert.Equal("#FF8000", color.HexRgb);

        // 8-digit hex updates A as well
        bool parsed8 = color.SetFromHex("#8000FF00");
        Assert.True(parsed8);
        Assert.Equal(0.0, color.R, 2);
        Assert.Equal(1.0, color.G, 2);
        Assert.Equal(0.0, color.B, 2);
        Assert.Equal(0.5, color.A, 2);

        // HexCode setter
        color.HexCode = "#FFFF0000";
        Assert.Equal(1.0, color.R, 2);
        Assert.Equal(0.0, color.G, 2);
        Assert.Equal(0.0, color.B, 2);
        Assert.Equal(1.0, color.A, 2);
    }

    [Fact]
    public void LightColor_ClampingAndByteProperties()
    {
        var color = new LightColor();
        color.R = 1.5;
        color.G = -0.5;
        color.B = 0.5;
        color.A = 2.0;

        Assert.Equal(1.0, color.R);
        Assert.Equal(0.0, color.G);
        Assert.Equal(0.5, color.B);
        Assert.Equal(1.0, color.A);

        Assert.Equal(255, color.RByte);
        Assert.Equal(0, color.GByte);
        Assert.Equal(128, color.BByte);
        Assert.Equal(255, color.AByte);
    }

    [Fact]
    public void MainWindowViewModel_ContainsLightingViewModel()
    {
        var mainWindowVm = new MainWindowViewModel(autoReopenLastRundown: false);
        Assert.NotNull(mainWindowVm.Lighting);
        Assert.Empty(mainWindowVm.Lighting.Blocks);
    }

    [Fact]
    public void LightingViewModel_CategoryFiltering_UpdatesCurrentCategorySettings()
    {
        var vm = new LightingViewModel(LightSettingsService.LoadVanillaBlocks());
        Assert.NotNull(vm.SelectedBlock);

        // Vanilla block 1 has General, Special, Emergency, Independent, Door, Sign, DoorImportant
        vm.SelectedCategory = LightCategory.General;
        Assert.True(vm.HasCurrentCategorySettings);
        Assert.All(vm.CurrentCategorySettings, item => Assert.Equal(LightCategory.General, item.Category));

        vm.SelectedCategory = LightCategory.Emergency;
        Assert.True(vm.HasCurrentCategorySettings);
        Assert.All(vm.CurrentCategorySettings, item => Assert.Equal(LightCategory.Emergency, item.Category));
    }

    [Fact]
    public void LightingViewModel_AddOption_UsesSelectedCategory()
    {
        var vm = new LightingViewModel(LightSettingsService.LoadVanillaBlocks());
        Assert.NotNull(vm.SelectedBlock);

        vm.SelectedCategory = LightCategory.Sign;
        int initialCount = vm.CurrentCategorySettings.Count;

        vm.AddOption();
        Assert.Equal(initialCount + 1, vm.CurrentCategorySettings.Count);
        var added = vm.CurrentCategorySettings.Last();
        Assert.Equal(LightCategory.Sign, added.Category);
        Assert.Contains(added, vm.SelectedBlock.LightCategorySettings);

        vm.RemoveOption(added);
        Assert.Equal(initialCount, vm.CurrentCategorySettings.Count);
        Assert.DoesNotContain(added, vm.SelectedBlock.LightCategorySettings);
    }

    [Fact]
    public void LightingViewModel_DetectsMissingCategoriesWarning()
    {
        var blockWithOnlyGeneral = new LightSettingsBlock
        {
            PersistentId = 12345,
            Name = "OnlyGeneral_Config",
            LightCategorySettings = new System.Collections.ObjectModel.ObservableCollection<LightCategorySettingItem>
            {
                new() { Category = LightCategory.General, On = true }
            }
        };

        var vm = new LightingViewModel(new[] { blockWithOnlyGeneral });
        Assert.NotNull(vm.SelectedBlock);

        Assert.True(vm.HasMissingCategories);
        Assert.Contains("Special", vm.MissingCategoriesWarning);
        Assert.Contains("Emergency", vm.MissingCategoriesWarning);
        Assert.Contains("Door", vm.MissingCategoriesWarning);

        // Add options for each missing category
        foreach (var cat in vm.Categories)
        {
            if (blockWithOnlyGeneral.LightCategorySettings.All(s => s.Category != cat))
            {
                vm.SelectedCategory = cat;
                vm.AddOption();
            }
        }

        Assert.False(vm.HasMissingCategories);
        Assert.Empty(vm.MissingCategoriesWarning);

        // Remove the option for Special
        var specialItem = blockWithOnlyGeneral.LightCategorySettings.First(s => s.Category == LightCategory.Special);
        vm.SelectedCategory = LightCategory.Special;
        vm.RemoveOption(specialItem);

        Assert.True(vm.HasMissingCategories);
        Assert.Contains("Special", vm.MissingCategoriesWarning);
    }
}
