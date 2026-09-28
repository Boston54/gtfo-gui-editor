using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ComplexResourceSetTests
{
    [Fact]
    public void LoadVanillaBlocks_LoadsAll79Blocks()
    {
        var blocks = ComplexResourceSetService.LoadVanillaBlocks();
        Assert.NotNull(blocks);
        Assert.Equal(79, blocks.Count);

        var firstBlock = blocks[0];
        Assert.Equal((uint)1, firstBlock.PersistentId);
        Assert.Equal("Complex_Mining", firstBlock.Name);
        Assert.Equal(ComplexType.Mining, firstBlock.ComplexType);
        Assert.Equal(SubComplexType.DigSite, firstBlock.PrimareSubComplexUsed);
        Assert.Equal(BundleNameType.Complex_Mining, firstBlock.BundleName);
        Assert.True(firstBlock.InternalEnabled);
        Assert.True(firstBlock.RandomizeGeomorphOrder);
    }

    [Fact]
    public void LoadVanillaBlocks_ParsesLevelGenConfigAndPrefabs()
    {
        var blocks = ComplexResourceSetService.LoadVanillaBlocks();
        var miningBlock = blocks.FirstOrDefault(b => b.PersistentId == 1);
        Assert.NotNull(miningBlock);

        Assert.NotNull(miningBlock.LevelGenConfig);
        Assert.Equal(40, miningBlock.LevelGenConfig.GridSize);
        Assert.Equal(64.0, miningBlock.LevelGenConfig.CellDimension);
        Assert.Equal(6.0, miningBlock.LevelGenConfig.AltitudeOffset);
        Assert.Equal(TransitionDirection.FloorUp, miningBlock.LevelGenConfig.TransitionDirection);
        Assert.Equal(LevelProgression.StartLevel, miningBlock.LevelGenConfig.LevelProgression);

        Assert.NotEmpty(miningBlock.GeomorphTiles_1x1);
        Assert.Contains(miningBlock.GeomorphTiles_1x1, g => g.Prefab.Contains("geo_64x64_mining_dig_site_AS_04"));
        Assert.Equal(SubComplexType.DigSite, miningBlock.GeomorphTiles_1x1[0].SubComplex);
        Assert.Equal(ShardType.S10, miningBlock.GeomorphTiles_1x1[0].Shard);
    }

    [Fact]
    public void CreateFreshBlock_InitializesDefaults()
    {
        var fresh = ComplexResourceSetService.CreateFreshBlock(1001, "Custom_Test_Block", ComplexType.Tech, SubComplexType.Lab);
        Assert.Equal((uint)1001, fresh.PersistentId);
        Assert.Equal("Custom_Test_Block", fresh.Name);
        Assert.Equal(ComplexType.Tech, fresh.ComplexType);
        Assert.Equal(SubComplexType.Lab, fresh.PrimareSubComplexUsed);
        Assert.Equal(BundleNameType.Complex_Tech, fresh.BundleName);
        Assert.True(fresh.InternalEnabled);
        Assert.NotNull(fresh.LevelGenConfig);
        Assert.Empty(fresh.GeomorphTiles_1x1);
    }

    [Fact]
    public void CreateBlockFromTemplate_ClonesPropertiesAndPrefabsDeeply()
    {
        var blocks = ComplexResourceSetService.LoadVanillaBlocks();
        var template = blocks.First(b => b.PersistentId == 1);

        var cloned = ComplexResourceSetService.CreateBlockFromTemplate(template, 2001, "Cloned_Mining_Block");

        Assert.Equal((uint)2001, cloned.PersistentId);
        Assert.Equal("Cloned_Mining_Block", cloned.Name);
        Assert.Equal(template.ComplexType, cloned.ComplexType);
        Assert.Equal(template.GeomorphTiles_1x1.Count, cloned.GeomorphTiles_1x1.Count);

        // Ensure deep clone (modifying clone does not mutate template)
        cloned.GeomorphTiles_1x1.Add(new PrefabItem { Prefab = "NewCustomPrefab.prefab", SubComplex = SubComplexType.DigSite, Shard = ShardType.S20 });
        Assert.NotEqual(template.GeomorphTiles_1x1.Count, cloned.GeomorphTiles_1x1.Count);

        cloned.LevelGenConfig.GridSize = 99;
        Assert.NotEqual(template.LevelGenConfig.GridSize, cloned.LevelGenConfig.GridSize);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_InitializesWithBlocks()
    {
        var vm = new ComplexResourceSetsViewModel();
        Assert.Equal(79, vm.Blocks.Count);
        Assert.NotNull(vm.SelectedBlock);
        Assert.Equal((uint)1, vm.SelectedBlock.PersistentId);
        Assert.True(vm.HasSelectedBlock);
        Assert.NotEmpty(vm.Categories);
        Assert.NotEmpty(vm.CurrentCategoryItems);
        Assert.Contains("79 blocks loaded", vm.StatusText);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_SelectsBlock_UpdatesPropertiesAndItems()
    {
        var vm = new ComplexResourceSetsViewModel();
        var block2 = vm.Blocks.FirstOrDefault(b => b.PersistentId == 102);
        Assert.NotNull(block2);

        vm.SelectedBlock = block2;
        Assert.Equal((uint)102, vm.SelectedBlock.PersistentId);
        Assert.Equal("Complex_Mining_R8B3", vm.SelectedBlock.Name);

        vm.SelectedCategory = nameof(ComplexResourceSetBlock.Ladders_4m);
        Assert.NotNull(vm.CurrentCategoryItems);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_AddAndRemovePrefabItem()
    {
        var vm = new ComplexResourceSetsViewModel();
        var initialCount = vm.CurrentCategoryItems.Count;

        vm.AddPrefabItem();
        Assert.Equal(initialCount + 1, vm.CurrentCategoryItems.Count);
        Assert.NotNull(vm.SelectedPrefabItem);

        vm.RemoveSelectedPrefabItem();
        Assert.Equal(initialCount, vm.CurrentCategoryItems.Count);
        Assert.Null(vm.SelectedPrefabItem);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_OpenCreateBlockDialog_GeneratesRandomPersistentId()
    {
        var vm = new ComplexResourceSetsViewModel();
        var existingIds = vm.Blocks.Select(b => b.PersistentId).ToHashSet();

        vm.OpenCreateBlockDialog();

        Assert.True(vm.IsCreatingBlock);
        Assert.True(vm.IsCreatingFresh);
        Assert.False(vm.IsCreatingFromTemplate);
        Assert.True(vm.NewBlockPersistentId > 0);
        Assert.DoesNotContain(vm.NewBlockPersistentId, existingIds);
        Assert.NotEmpty(vm.NewBlockName);
        Assert.False(vm.HasCreationErrorMessage);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_DuplicatePersistentIdWarning_ShownWhenDuplicateExists()
    {
        var vm = new ComplexResourceSetsViewModel();
        Assert.False(vm.HasDuplicatePersistentId);
        Assert.Empty(vm.DuplicatePersistentIdWarning);

        // Change selected block's persistent ID to match another block's ID
        var otherBlock = vm.Blocks.First(b => b.PersistentId != vm.SelectedBlock!.PersistentId);
        vm.SelectedBlock!.PersistentId = otherBlock.PersistentId;

        Assert.True(vm.HasDuplicatePersistentId);
        Assert.Contains("Warning", vm.DuplicatePersistentIdWarning);
        Assert.Contains(otherBlock.PersistentId.ToString(), vm.DuplicatePersistentIdWarning);

        // Change back to unique ID
        vm.SelectedBlock.PersistentId = 999999;
        Assert.False(vm.HasDuplicatePersistentId);
        Assert.Empty(vm.DuplicatePersistentIdWarning);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_EnumsContainRequiredValues()
    {
        Assert.Equal(3, ComplexResourceSetsViewModel.AvailableComplexTypes.Length);
        Assert.Contains(ComplexType.Mining, ComplexResourceSetsViewModel.AvailableComplexTypes);
        Assert.Contains(ComplexType.Service, ComplexResourceSetsViewModel.AvailableComplexTypes);
        Assert.Contains(ComplexType.Tech, ComplexResourceSetsViewModel.AvailableComplexTypes);

        Assert.Equal(13, ComplexResourceSetsViewModel.AvailableSubComplexTypes.Length);
        Assert.Contains(SubComplexType.DigSite, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.Refinery, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.Storage, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.DataCenter, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.Lab, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.All, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.Floodways, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.Mining_Reactor, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.Plug_SubComplex_Transition, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.Tech_Reactor, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.Tech_Portal, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.Gardens, ComplexResourceSetsViewModel.AvailableSubComplexTypes);
        Assert.Contains(SubComplexType.Mining_Portal, ComplexResourceSetsViewModel.AvailableSubComplexTypes);

        Assert.Equal(5, ComplexResourceSetsViewModel.AvailableBundleNames.Length);
        Assert.Contains(BundleNameType.None, ComplexResourceSetsViewModel.AvailableBundleNames);
        Assert.Contains(BundleNameType.Complex_Shared, ComplexResourceSetsViewModel.AvailableBundleNames);
        Assert.Contains(BundleNameType.Complex_Mining, ComplexResourceSetsViewModel.AvailableBundleNames);
        Assert.Contains(BundleNameType.Complex_Tech, ComplexResourceSetsViewModel.AvailableBundleNames);
        Assert.Contains(BundleNameType.Complex_Service, ComplexResourceSetsViewModel.AvailableBundleNames);

        Assert.Equal(2, ComplexResourceSetsViewModel.AvailableTransitionDirections.Length);
        Assert.Contains(TransitionDirection.FloorUp, ComplexResourceSetsViewModel.AvailableTransitionDirections);
        Assert.Contains(TransitionDirection.FloorDown, ComplexResourceSetsViewModel.AvailableTransitionDirections);

        Assert.Equal(20, ComplexResourceSetsViewModel.AvailableShards.Length);
        Assert.Equal(ShardType.S1, ComplexResourceSetsViewModel.AvailableShards[0]);
        Assert.Equal(ShardType.S20, ComplexResourceSetsViewModel.AvailableShards[19]);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_ConfirmCreateBlock_CreatesFreshBlock()
    {
        var vm = new ComplexResourceSetsViewModel();
        var initialCount = vm.Blocks.Count;

        vm.OpenCreateBlockDialog();
        vm.IsCreatingFresh = true;
        vm.NewBlockPersistentId = 5001;
        vm.NewBlockName = "Brand_New_Set";

        bool success = vm.ConfirmCreateBlock();

        Assert.True(success);
        Assert.False(vm.IsCreatingBlock);
        Assert.Equal(initialCount + 1, vm.Blocks.Count);
        Assert.NotNull(vm.SelectedBlock);
        Assert.Equal((uint)5001, vm.SelectedBlock.PersistentId);
        Assert.Equal("Brand_New_Set", vm.SelectedBlock.Name);
        Assert.Empty(vm.SelectedBlock.GeomorphTiles_1x1);
        Assert.Contains("Created block [5001] Brand_New_Set", vm.StatusText);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_ConfirmCreateBlock_CreatesFromTemplate()
    {
        var vm = new ComplexResourceSetsViewModel();
        var initialCount = vm.Blocks.Count;

        vm.OpenCreateBlockDialog();
        vm.IsCreatingFromTemplate = true;
        vm.SelectedTemplate = vm.AvailableTemplates.First(t => t.PersistentId == 1);
        vm.NewBlockPersistentId = 5002;
        vm.NewBlockName = "Template_Derived_Set";

        bool success = vm.ConfirmCreateBlock();

        Assert.True(success);
        Assert.False(vm.IsCreatingBlock);
        Assert.Equal(initialCount + 1, vm.Blocks.Count);
        Assert.NotNull(vm.SelectedBlock);
        Assert.Equal((uint)5002, vm.SelectedBlock.PersistentId);
        Assert.Equal("Template_Derived_Set", vm.SelectedBlock.Name);
        Assert.NotEmpty(vm.SelectedBlock.GeomorphTiles_1x1);
        Assert.Contains("Created block [5002] Template_Derived_Set", vm.StatusText);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_ConfirmCreateBlock_ValidatesInputs()
    {
        var vm = new ComplexResourceSetsViewModel();

        // Empty Name
        vm.OpenCreateBlockDialog();
        vm.NewBlockName = "   ";
        Assert.False(vm.ConfirmCreateBlock());
        Assert.True(vm.HasCreationErrorMessage);
        Assert.Contains("name cannot be empty", vm.CreationErrorMessage);

        // Zero ID
        vm.NewBlockName = "Valid Name";
        vm.NewBlockPersistentId = 0;
        Assert.False(vm.ConfirmCreateBlock());
        Assert.True(vm.HasCreationErrorMessage);
        Assert.Contains("greater than 0", vm.CreationErrorMessage);

        // Duplicate ID
        vm.NewBlockPersistentId = 1; // existing block
        Assert.False(vm.ConfirmCreateBlock());
        Assert.True(vm.HasCreationErrorMessage);
        Assert.Contains("already exists", vm.CreationErrorMessage);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_CancelCreateBlock_ResetsState()
    {
        var vm = new ComplexResourceSetsViewModel();
        vm.OpenCreateBlockDialog();
        Assert.True(vm.IsCreatingBlock);

        vm.CancelCreateBlock();
        Assert.False(vm.IsCreatingBlock);
        Assert.False(vm.HasCreationErrorMessage);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_DeleteSelectedBlock_RemovesBlockAndUpdatesSelection()
    {
        var vm = new ComplexResourceSetsViewModel();
        var initialCount = vm.Blocks.Count;
        var firstBlock = vm.Blocks[0];
        var secondBlock = vm.Blocks[1];

        vm.SelectedBlock = firstBlock;
        bool deleted = vm.DeleteSelectedBlock();

        Assert.True(deleted);
        Assert.Equal(initialCount - 1, vm.Blocks.Count);
        Assert.DoesNotContain(firstBlock, vm.Blocks);
        Assert.Equal(secondBlock, vm.SelectedBlock);
        Assert.Contains("Deleted block [1]", vm.StatusText);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_DeleteSelectedBlock_WhenNull_ReturnsFalse()
    {
        var vm = new ComplexResourceSetsViewModel(System.Array.Empty<ComplexResourceSetBlock>());
        Assert.Null(vm.SelectedBlock);
        Assert.False(vm.HasSelectedBlock);

        bool deleted = vm.DeleteSelectedBlock();
        Assert.False(deleted);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_DeleteSelectedBlock_WhenLastBlockRemoved_SetsSelectedBlockNull()
    {
        var singleBlock = ComplexResourceSetService.CreateFreshBlock(999, "SoloBlock");
        var vm = new ComplexResourceSetsViewModel(new[] { singleBlock });

        Assert.Single(vm.Blocks);
        Assert.Equal(singleBlock, vm.SelectedBlock);

        bool deleted = vm.DeleteSelectedBlock();

        Assert.True(deleted);
        Assert.Empty(vm.Blocks);
        Assert.Null(vm.SelectedBlock);
        Assert.False(vm.HasSelectedBlock);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_RequestDeleteSelectedBlock_OpensConfirmation()
    {
        var vm = new ComplexResourceSetsViewModel();
        var selected = vm.SelectedBlock;
        Assert.NotNull(selected);

        vm.RequestDeleteSelectedBlock();

        Assert.True(vm.IsConfirmingDelete);
        Assert.Equal(selected, vm.BlockToDelete);
        Assert.Contains(selected.PersistentId.ToString(), vm.DeleteConfirmationMessage);
        Assert.Contains(selected.Name, vm.DeleteConfirmationMessage);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_ConfirmDeleteBlock_RemovesBlockAndClosesDialog()
    {
        var vm = new ComplexResourceSetsViewModel();
        var initialCount = vm.Blocks.Count;
        var selected = vm.SelectedBlock;
        Assert.NotNull(selected);

        vm.RequestDeleteSelectedBlock();
        Assert.True(vm.IsConfirmingDelete);

        bool deleted = vm.ConfirmDeleteBlock();

        Assert.True(deleted);
        Assert.False(vm.IsConfirmingDelete);
        Assert.Null(vm.BlockToDelete);
        Assert.Equal(initialCount - 1, vm.Blocks.Count);
        Assert.DoesNotContain(selected, vm.Blocks);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_CancelDeleteBlock_ClosesDialogWithoutDeleting()
    {
        var vm = new ComplexResourceSetsViewModel();
        var initialCount = vm.Blocks.Count;
        var selected = vm.SelectedBlock;
        Assert.NotNull(selected);

        vm.RequestDeleteSelectedBlock();
        Assert.True(vm.IsConfirmingDelete);
        Assert.Equal(selected, vm.BlockToDelete);

        vm.CancelDeleteBlock();

        Assert.False(vm.IsConfirmingDelete);
        Assert.Null(vm.BlockToDelete);
        Assert.Equal(initialCount, vm.Blocks.Count);
        Assert.Contains(selected, vm.Blocks);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_RequestDeleteSelectedBlock_WhenNoSelection_DoesNotOpenDialog()
    {
        var vm = new ComplexResourceSetsViewModel(System.Array.Empty<ComplexResourceSetBlock>());
        Assert.Null(vm.SelectedBlock);

        vm.RequestDeleteSelectedBlock();

        Assert.False(vm.IsConfirmingDelete);
        Assert.Null(vm.BlockToDelete);
    }

    [Fact]
    public void LevelProgression_Enum_HasExpectedValuesAndViewModelExposure()
    {
        var values = ComplexResourceSetsViewModel.AvailableLevelProgressions;
        Assert.Equal(3, values.Length);
        Assert.Contains(LevelProgression.StartLevel, values);
        Assert.Contains(LevelProgression.MidLevel, values);
        Assert.Contains(LevelProgression.EndLevel, values);

        var vm = new ComplexResourceSetsViewModel();
        Assert.Equal(values, vm.LevelProgressions);

        var config = new LevelGenConfig
        {
            LevelProgression = LevelProgression.MidLevel
        };
        var cloned = config.Clone();
        Assert.Equal(LevelProgression.MidLevel, cloned.LevelProgression);

        config.LevelProgression = LevelProgression.EndLevel;
        Assert.Equal(LevelProgression.EndLevel, config.LevelProgression);
        Assert.Equal(LevelProgression.MidLevel, cloned.LevelProgression);
    }
}
