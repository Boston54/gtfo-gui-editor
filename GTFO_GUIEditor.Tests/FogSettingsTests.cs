using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class FogSettingsTests
{
    [Fact]
    public void LoadVanillaBlocks_LoadsBlocksSuccessfully()
    {
        var blocks = FogSettingsService.LoadVanillaBlocks();
        Assert.NotNull(blocks);
        Assert.NotEmpty(blocks);

        var elevatorFog = blocks.FirstOrDefault(b => b.PersistentId == 21);
        Assert.NotNull(elevatorFog);
        Assert.Equal("Fog_ELEVATOR", elevatorFog.Name);
        Assert.True(elevatorFog.InternalEnabled);
        Assert.True(elevatorFog.Enabled);
        Assert.NotNull(elevatorFog.FogColor);
        Assert.NotNull(elevatorFog.DensityNoiseDirection);
    }

    [Fact]
    public void CreateFreshBlock_InitializesDefaults()
    {
        var fresh = FogSettingsService.CreateFreshBlock(999, "Custom_Fog_Test");
        Assert.Equal((uint)999, fresh.PersistentId);
        Assert.Equal("Custom_Fog_Test", fresh.Name);
        Assert.True(fresh.InternalEnabled);
        Assert.True(fresh.Enabled);
        Assert.NotNull(fresh.FogColor);
        Assert.NotNull(fresh.DensityNoiseDirection);
        Assert.Equal(0.0, fresh.FogDensity);
        Assert.Equal(0.0, fresh.Infection);
    }

    [Fact]
    public void CreateBlockFromTemplate_ClonesDeeply()
    {
        var blocks = FogSettingsService.LoadVanillaBlocks();
        var template = blocks.First(b => b.PersistentId == 21);

        var cloned = FogSettingsService.CreateBlockFromTemplate(template, 2026, "Cloned_Fog_Config");

        Assert.Equal((uint)2026, cloned.PersistentId);
        Assert.Equal("Cloned_Fog_Config", cloned.Name);
        Assert.Equal(template.FogDensity, cloned.FogDensity);
        Assert.Equal(template.Infection, cloned.Infection);

        // Modifying clone does not alter template
        cloned.FogColor.R = 0.1234;
        Assert.NotEqual(template.FogColor.R, cloned.FogColor.R);

        cloned.DensityNoiseDirection.X = 99.9;
        Assert.NotEqual(template.DensityNoiseDirection.X, cloned.DensityNoiseDirection.X);
    }

    [Fact]
    public void FogSettingsViewModel_InitializesAndSwapsConfigs()
    {
        var vm = new FogSettingsViewModel();
        Assert.NotEmpty(vm.Blocks);
        Assert.NotNull(vm.SelectedBlock);
        Assert.True(vm.HasSelectedBlock);
        Assert.Contains("configs loaded", vm.StatusText);

        var targetBlock = vm.Blocks.FirstOrDefault(b => b.PersistentId == 21);
        Assert.NotNull(targetBlock);

        vm.SelectedBlock = targetBlock;
        Assert.Equal((uint)21, vm.SelectedBlock.PersistentId);
        Assert.Equal("Fog_ELEVATOR", vm.SelectedBlock.Name);
        Assert.Contains("Fog_ELEVATOR", vm.StatusText);
    }

    [Fact]
    public void FogSettingsViewModel_CreateBlock_ValidatesInputsAndAdds()
    {
        var vm = new FogSettingsViewModel();
        int initialCount = vm.Blocks.Count;

        vm.OpenCreateBlockDialog();
        Assert.True(vm.IsCreatingBlock);
        Assert.True(vm.IsCreatingFresh);

        // Try creating with empty name
        vm.NewBlockName = "   ";
        Assert.False(vm.ConfirmCreateBlock());
        Assert.True(vm.HasCreationErrorMessage);

        // Try creating with duplicate ID
        vm.NewBlockName = "Valid Name";
        vm.NewBlockPersistentId = vm.Blocks[0].PersistentId;
        Assert.False(vm.ConfirmCreateBlock());
        Assert.True(vm.HasCreationErrorMessage);

        // Valid creation
        uint uniqueId = 888999;
        vm.NewBlockPersistentId = uniqueId;
        vm.NewBlockName = "Brand New Fog";
        Assert.True(vm.ConfirmCreateBlock());
        Assert.False(vm.IsCreatingBlock);
        Assert.Equal(initialCount + 1, vm.Blocks.Count);
        Assert.NotNull(vm.SelectedBlock);
        Assert.Equal(uniqueId, vm.SelectedBlock.PersistentId);
        Assert.Equal("Brand New Fog", vm.SelectedBlock.Name);
    }

    [Fact]
    public void FogSettingsViewModel_DeleteBlock_RemovesAndUpdatesSelection()
    {
        var vm = new FogSettingsViewModel();
        var block = FogSettingsService.CreateFreshBlock(777111, "To Delete");
        vm.Blocks.Add(block);
        vm.SelectedBlock = block;
        int count = vm.Blocks.Count;

        vm.RequestDeleteSelectedBlock();
        Assert.True(vm.IsConfirmingDelete);
        Assert.Equal(block, vm.BlockToDelete);
        Assert.Contains("To Delete", vm.DeleteConfirmationMessage);

        vm.ConfirmDeleteBlock();
        Assert.False(vm.IsConfirmingDelete);
        Assert.Equal(count - 1, vm.Blocks.Count);
        Assert.DoesNotContain(block, vm.Blocks);
    }

    [Fact]
    public void FogSettingsViewModel_DuplicateIdWarning_DetectsDuplicates()
    {
        var vm = new FogSettingsViewModel();
        var block1 = FogSettingsService.CreateFreshBlock(55555, "Fog A");
        var block2 = FogSettingsService.CreateFreshBlock(55555, "Fog B");

        vm.Blocks.Add(block1);
        vm.Blocks.Add(block2);
        vm.SelectedBlock = block1;

        Assert.True(vm.HasDuplicatePersistentId);
        Assert.Contains("55555", vm.DuplicatePersistentIdWarning);
    }

    [Fact]
    public void SectorSettings_FogTransitionProperties_AreRetainedAndCloned()
    {
        var sector = new SectorSettings
        {
            FogTransitionDataOnGotoWin = 174,
            FogTransitionDataOnGotoWinDuration = 15.5
        };

        Assert.Equal((uint)174, sector.FogTransitionDataOnGotoWin);
        Assert.Equal(15.5, sector.FogTransitionDataOnGotoWinDuration);

        var cloned = sector.Clone();
        Assert.Equal((uint)174, cloned.FogTransitionDataOnGotoWin);
        Assert.Equal(15.5, cloned.FogTransitionDataOnGotoWinDuration);

        cloned.FogTransitionDataOnGotoWin = 200;
        cloned.FogTransitionDataOnGotoWinDuration = 30.0;
        Assert.Equal((uint)174, sector.FogTransitionDataOnGotoWin);
        Assert.Equal(15.5, sector.FogTransitionDataOnGotoWinDuration);
    }

    [Fact]
    public void LevelLayoutViewModel_AvailableFogTransitionOptions_IncludesDoNotTransitionOption()
    {
        var fogBlocks = new System.Collections.ObjectModel.ObservableCollection<FogSettingsBlock>
        {
            FogSettingsService.CreateFreshBlock(101, "Test_Fog_1"),
            FogSettingsService.CreateFreshBlock(102, "Test_Fog_2")
        };

        var vm = new LevelLayoutViewModel(fogSettings: fogBlocks);
        Assert.NotNull(vm.AvailableFogTransitionOptions);
        Assert.NotEmpty(vm.AvailableFogTransitionOptions);

        // First item must be the "Do not transition" option with ID 0
        var defaultOption = vm.AvailableFogTransitionOptions[0];
        Assert.Equal((uint)0, defaultOption.PersistentId);
        Assert.Equal("Do not transition", defaultOption.DisplayText);

        // Next items are the fog blocks
        Assert.Equal((uint)101, vm.AvailableFogTransitionOptions[1].PersistentId);
        Assert.Equal("[101] Test_Fog_1", vm.AvailableFogTransitionOptions[1].DisplayText);
        Assert.Equal((uint)102, vm.AvailableFogTransitionOptions[2].PersistentId);
        Assert.Equal("[102] Test_Fog_2", vm.AvailableFogTransitionOptions[2].DisplayText);

        // When a new fog block is added, options update
        fogBlocks.Add(FogSettingsService.CreateFreshBlock(103, "Test_Fog_3"));
        Assert.Equal(4, vm.AvailableFogTransitionOptions.Count);
        Assert.Equal((uint)103, vm.AvailableFogTransitionOptions[3].PersistentId);
    }
}
