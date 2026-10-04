using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class LevelLayoutTests
{
    [Fact]
    public void LevelLayoutBlock_DefaultsAndClone_WorkCorrectly()
    {
        var block = new LevelLayoutBlock
        {
            Name = "Mining Run A",
            PersistentId = 42,
            ComplexResourceSet = 1,
            ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_mining_02.prefab",
            InternalEnabled = true
        };

        Assert.Equal("Mining Run A", block.DisplayText);

        var clone = block.Clone();
        Assert.Equal(block.Name, clone.Name);
        Assert.Equal(block.PersistentId, clone.PersistentId);
        Assert.Equal(block.ComplexResourceSet, clone.ComplexResourceSet);
        Assert.Equal(block.ElevatorTile, clone.ElevatorTile);
        Assert.Equal(block.InternalEnabled, clone.InternalEnabled);

        clone.Name = "Mining Run B";
        Assert.NotEqual(block.Name, clone.Name);
    }

    [Fact]
    public void LevelLayoutBlock_DisplayText_HandlesEmptyName()
    {
        var block = new LevelLayoutBlock
        {
            Name = "",
            PersistentId = 100
        };

        Assert.Equal("Level 100", block.DisplayText);
    }

    [Fact]
    public void LevelLayoutViewModel_InitializesWithDefaultLevel()
    {
        var vm = new LevelLayoutViewModel();

        Assert.NotEmpty(vm.Levels);
        Assert.NotNull(vm.SelectedLevel);
        Assert.True(vm.HasSelectedLevel);
        Assert.NotEmpty(vm.AvailableComplexResourceSets);
        Assert.False(vm.HasDuplicatePersistentId);
    }

    [Fact]
    public void LevelLayoutViewModel_SelectingComplexResourceSet_UpdatesElevatorTiles()
    {
        var set1 = new ComplexResourceSetBlock
        {
            Name = "Set 1",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/Mining/Elevator1.prefab" },
                new PrefabItem { Prefab = "Assets/Mining/Elevator2.prefab" }
            }
        };

        var set2 = new ComplexResourceSetBlock
        {
            Name = "Set 2",
            PersistentId = 2,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/Tech/ElevatorA.prefab" }
            }
        };

        var sets = new ObservableCollection<ComplexResourceSetBlock> { set1, set2 };
        var vm = new LevelLayoutViewModel(sets, new List<LevelLayoutBlock>());

        vm.OpenCreateLevelDialog();
        vm.SelectedComplexResourceSet = set1;

        Assert.Equal(2, vm.AvailableElevatorTiles.Count);
        Assert.Contains(vm.AvailableElevatorTiles, opt => opt.Prefab == "Assets/Mining/Elevator1.prefab");
        Assert.Contains(vm.AvailableElevatorTiles, opt => opt.Prefab == "Assets/Mining/Elevator2.prefab");
        Assert.Equal("Assets/Mining/Elevator1.prefab", vm.SelectedElevatorTile?.Prefab);

        // Switch to set 2
        vm.SelectedComplexResourceSet = set2;
        Assert.Single(vm.AvailableElevatorTiles);
        Assert.Equal("Assets/Tech/ElevatorA.prefab", vm.SelectedElevatorTile?.Prefab);
    }

    [Fact]
    public void LevelLayoutViewModel_ConfirmCreateLevel_CreatesAndSelectsLevel()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Tech Set",
            PersistentId = 10,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/Tech/Elevator01.prefab" }
            }
        };

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock>());

        vm.OpenCreateLevelDialog();
        vm.NewLevelName = "Level Alpha";
        vm.NewLevelPersistentId = 500;
        vm.SelectedComplexResourceSet = set;

        bool success = vm.ConfirmCreateLevel();

        Assert.True(success);
        Assert.False(vm.IsCreatingLevel);
        Assert.Single(vm.Levels);
        Assert.NotNull(vm.SelectedLevel);
        Assert.Equal("Level Alpha", vm.SelectedLevel.Name);
        Assert.Equal((uint)500, vm.SelectedLevel.PersistentId);
        Assert.Equal((uint)10, vm.SelectedLevel.ComplexResourceSet);
        Assert.Equal("Assets/Tech/Elevator01.prefab", vm.SelectedLevel.ElevatorTile);
    }

    [Fact]
    public void LevelLayoutViewModel_ConfirmCreateLevel_ValidatesInput()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Mining Set",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/Mining/Elevator01.prefab" }
            }
        };

        var existingLevel = new LevelLayoutBlock { Name = "Existing", PersistentId = 100 };
        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { existingLevel });

        vm.OpenCreateLevelDialog();

        // Empty Name
        vm.NewLevelName = "   ";
        Assert.False(vm.ConfirmCreateLevel());
        Assert.True(vm.HasCreationErrorMessage);

        // PersistentId = 0
        vm.NewLevelName = "Valid Name";
        vm.NewLevelPersistentId = 0;
        Assert.False(vm.ConfirmCreateLevel());
        Assert.True(vm.HasCreationErrorMessage);

        // Duplicate PersistentId
        vm.NewLevelPersistentId = 100;
        Assert.False(vm.ConfirmCreateLevel());
        Assert.Contains("already exists", vm.CreationErrorMessage);

        // Missing Elevator Tile
        vm.NewLevelPersistentId = 200;
        vm.SelectedElevatorTile = null;
        Assert.False(vm.ConfirmCreateLevel());
        Assert.True(vm.HasCreationErrorMessage);

        // Cancel dialog
        vm.CancelCreateLevel();
        Assert.False(vm.IsCreatingLevel);
    }

    [Fact]
    public void LevelLayoutViewModel_DeleteLevelWorkflow_WorksCorrectly()
    {
        var level1 = new LevelLayoutBlock { Name = "L1", PersistentId = 1 };
        var level2 = new LevelLayoutBlock { Name = "L2", PersistentId = 2 };

        var vm = new LevelLayoutViewModel(null, new List<LevelLayoutBlock> { level1, level2 });
        vm.SelectedLevel = level1;

        vm.RequestDeleteSelectedLevel();
        Assert.True(vm.IsConfirmingDelete);
        Assert.Equal(level1, vm.LevelToDelete);
        Assert.Contains("L1", vm.DeleteConfirmationMessage);

        // Cancel delete
        vm.CancelDeleteLevel();
        Assert.False(vm.IsConfirmingDelete);
        Assert.Equal(2, vm.Levels.Count);

        // Request again and confirm delete
        vm.RequestDeleteSelectedLevel();
        bool deleted = vm.ConfirmDeleteLevel();

        Assert.True(deleted);
        Assert.False(vm.IsConfirmingDelete);
        Assert.Single(vm.Levels);
        Assert.Equal(level2, vm.SelectedLevel);
    }

    [Fact]
    public void LevelLayoutViewModel_DetectsDuplicatePersistentId()
    {
        var level1 = new LevelLayoutBlock { Name = "L1", PersistentId = 10 };
        var level2 = new LevelLayoutBlock { Name = "L2", PersistentId = 20 };

        var vm = new LevelLayoutViewModel(null, new List<LevelLayoutBlock> { level1, level2 });
        vm.SelectedLevel = level1;

        Assert.False(vm.HasDuplicatePersistentId);
        Assert.Empty(vm.DuplicatePersistentIdWarning);

        // Change level2 ID to match level1
        level2.PersistentId = 10;

        Assert.True(vm.HasDuplicatePersistentId);
        Assert.Contains("Warning: Persistent ID 10 is shared", vm.DuplicatePersistentIdWarning);
    }

    [Fact]
    public void MainWindowViewModel_InitializesLevelLayoutWithComplexResourceSets()
    {
        var mainVm = new MainWindowViewModel();

        Assert.NotNull(mainVm.LevelLayout);
        Assert.NotNull(mainVm.ComplexResourceSets);
        Assert.Equal(mainVm.ComplexResourceSets.Blocks, mainVm.LevelLayout.AvailableComplexResourceSets);
    }

    [Fact]
    public void DimensionLayoutBlock_PropertiesAndClone_WorkCorrectly()
    {
        var dim = new DimensionLayoutBlock
        {
            DimensionIndex = "Dimension_1",
            Name = "Dimension 1",
            ComplexResourceSet = 47,
            DimensionGeomorph = "Assets/AssetPrefabs/Complex/Dimensions/Desert/Dimension_Jungle_E2.prefab",
            TileCategory = "Dimension Geomorph",
            InternalEnabled = true,
            LevelLayoutData = 100
        };

        Assert.Equal("Dimension 1", dim.DisplayText);
        Assert.Equal((uint)47, dim.DimensionResourceSetID);

        var clone = dim.Clone();
        Assert.Equal(dim.DimensionIndex, clone.DimensionIndex);
        Assert.Equal(dim.Name, clone.Name);
        Assert.Equal(dim.ComplexResourceSet, clone.ComplexResourceSet);
        Assert.Equal(dim.DimensionGeomorph, clone.DimensionGeomorph);
        Assert.Equal(dim.TileCategory, clone.TileCategory);
        Assert.Equal(dim.LevelLayoutData, clone.LevelLayoutData);
        Assert.Equal(dim.InternalEnabled, clone.InternalEnabled);
    }

    [Fact]
    public void LevelLayoutBlock_DimensionsAndDimensionGeomorph_WorkCorrectly()
    {
        var level = new LevelLayoutBlock
        {
            Name = "Complex Level",
            PersistentId = 42,
            ComplexResourceSet = 1,
            ElevatorTile = "Assets/Mining/Elevator01.prefab"
        };

        Assert.Equal("Assets/Mining/Elevator01.prefab", level.DimensionGeomorph);
        Assert.True(level.HasDimension("Reality"));
        Assert.False(level.HasDimension("Dimension_1"));

        var realityDim = level.GetDimension("Reality");
        Assert.NotNull(realityDim);
        Assert.Equal("Reality", realityDim.DimensionIndex);
        Assert.Equal((uint)1, realityDim.ComplexResourceSet);
        Assert.Equal("Assets/Mining/Elevator01.prefab", realityDim.DimensionGeomorph);

        var dim1 = new DimensionLayoutBlock
        {
            DimensionIndex = "Dimension_1",
            Name = "Dimension 1",
            ComplexResourceSet = 104,
            DimensionGeomorph = "Assets/Mining/ObjectiveReactor.prefab"
        };

        level.SetDimension(dim1);
        Assert.True(level.HasDimension("Dimension_1"));
        Assert.Equal(dim1, level.GetDimension("Dimension_1"));

        var clone = level.Clone();
        Assert.Single(clone.Dimensions);
        Assert.True(clone.HasDimension("Dimension_1"));
        Assert.Equal("Assets/Mining/ObjectiveReactor.prefab", clone.GetDimension("Dimension_1")?.DimensionGeomorph);

        bool removed = level.RemoveDimension("Dimension_1");
        Assert.True(removed);
        Assert.False(level.HasDimension("Dimension_1"));
    }

    [Fact]
    public void LevelLayoutViewModel_DimensionDropdown_InitiallyOnlyContainsReality_UnlessDimensionsExist()
    {
        var vm = new LevelLayoutViewModel();

        Assert.NotEmpty(vm.AvailableDimensions);
        Assert.Single(vm.AvailableDimensions);
        Assert.Equal("Reality", vm.AvailableDimensions[0].DimensionIndex);
        Assert.Equal("Reality", vm.SelectedDimension?.DimensionIndex);
        Assert.True(vm.IsViewingReality);
        Assert.True(vm.CanAddDimension);
    }

    [Fact]
    public void LevelLayoutViewModel_AddDimension_AddsWithCustomName()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Desert Resource Set",
            PersistentId = 47,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/Desert/Elevator_D1.prefab" }
            },
            CustomGeomorphs_Objective_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/AssetPrefabs/Complex/Dimensions/Desert/Dimension_Jungle_E2.prefab" }
            }
        };

        var level = new LevelLayoutBlock { Name = "Desert Level", PersistentId = 20, ComplexResourceSet = 47, ElevatorTile = "Assets/Desert/Elevator_D1.prefab" };
        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        Assert.Single(vm.AvailableDimensions);

        vm.OpenAddDimensionDialog();
        Assert.True(vm.IsConfiguringDimension);
        Assert.Equal("Dimension 1", vm.DimensionCustomName);

        // Customize the dimension name
        vm.DimensionCustomName = "Jungle Subsector";
        vm.SelectedTileSource = "Dimension Geomorphs (Custom Objective 1x1)";
        vm.SelectedDimensionTile = vm.AvailableDimensionTiles.First();

        bool confirmed = vm.ConfirmPromptDimension();
        Assert.True(confirmed);
        Assert.False(vm.IsConfiguringDimension);

        // Verify available dimensions in dropdown
        Assert.Equal(2, vm.AvailableDimensions.Count);
        Assert.Equal("Reality", vm.AvailableDimensions[0].DisplayName);
        Assert.Equal("Jungle Subsector", vm.AvailableDimensions[1].DisplayName);
        Assert.Equal("Dimension_1", vm.AvailableDimensions[1].DimensionIndex);

        // Verify active dimension
        Assert.False(vm.IsViewingReality);
        Assert.Equal("Dimension_1", vm.ActiveDimensionIndex);
        Assert.Equal("Jungle Subsector", vm.ActiveDimensionDisplayName);
        Assert.Equal("Dimension: Jungle Subsector", vm.ActiveDimensionTitle);

        // Verify stored in level
        var dim = level.GetDimension("Dimension_1");
        Assert.NotNull(dim);
        Assert.Equal("Jungle Subsector", dim.Name);
    }

    [Fact]
    public void LevelLayoutViewModel_Max20DimensionsLimit()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Set",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem> { new PrefabItem { Prefab = "Elevator.prefab" } }
        };

        var level = new LevelLayoutBlock { Name = "Level", PersistentId = 1, ComplexResourceSet = 1, ElevatorTile = "Elevator.prefab" };
        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        for (int i = 1; i <= 20; i++)
        {
            Assert.True(vm.CanAddDimension);
            vm.OpenAddDimensionDialog();
            vm.DimensionCustomName = $"Sector {i}";
            vm.ConfirmPromptDimension();
        }

        Assert.Equal(20, level.Dimensions.Count);
        Assert.Equal(21, vm.AvailableDimensions.Count); // Reality + 20 dimensions
        Assert.False(vm.CanAddDimension);

        // Attempting to open add dimension dialog when at limit does nothing
        vm.OpenAddDimensionDialog();
        Assert.False(vm.IsConfiguringDimension);

        // Removing a dimension allows adding again
        vm.DeleteActiveDimension();
        Assert.Equal(19, level.Dimensions.Count);
        Assert.True(vm.CanAddDimension);
    }

    [Fact]
    public void LevelLayoutViewModel_CancelPromptDimension_RevertsSelection()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Set",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem> { new PrefabItem { Prefab = "Elevator.prefab" } }
        };
        var level = new LevelLayoutBlock { Name = "Level", PersistentId = 1, ComplexResourceSet = 1, ElevatorTile = "Elevator.prefab" };
        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        Assert.Equal("Reality", vm.SelectedDimension?.DimensionIndex);

        vm.OpenAddDimensionDialog();
        Assert.True(vm.IsConfiguringDimension);

        vm.CancelPromptDimension();
        Assert.False(vm.IsConfiguringDimension);
        Assert.Equal("Reality", vm.SelectedDimension?.DimensionIndex);
        Assert.True(vm.IsViewingReality);
        Assert.False(level.HasDimension("Dimension_1"));
    }

    [Fact]
    public void LevelLayoutViewModel_DeleteDimension_RemovesAndSwapsBackToReality()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Set",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem> { new PrefabItem { Prefab = "RealityElevator.prefab" } }
        };
        var level = new LevelLayoutBlock { Name = "Level", PersistentId = 1, ComplexResourceSet = 1, ElevatorTile = "RealityElevator.prefab" };
        var dim1 = new DimensionLayoutBlock { DimensionIndex = "Dimension_1", Name = "Sub Lab", ComplexResourceSet = 1, DimensionGeomorph = "Dim1Geo.prefab" };
        level.SetDimension(dim1);

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        Assert.Equal(2, vm.AvailableDimensions.Count);
        Assert.Equal("Sub Lab", vm.AvailableDimensions[1].DisplayName);

        var dim1Option = vm.AvailableDimensions.First(d => d.DimensionIndex == "Dimension_1");
        vm.SelectedDimension = dim1Option;

        Assert.False(vm.IsConfiguringDimension);
        Assert.Equal("Dimension_1", vm.ActiveDimensionIndex);
        Assert.Equal("Sub Lab", vm.ActiveDimensionDisplayName);
        Assert.True(vm.CanDeleteActiveDimension);

        vm.DeleteActiveDimension();

        Assert.False(level.HasDimension("Dimension_1"));
        Assert.True(vm.IsViewingReality);
        Assert.Equal("Reality", vm.SelectedDimension?.DimensionIndex);
        Assert.Single(vm.AvailableDimensions);
    }

    [Fact]
    public void LevelLayoutViewModel_ChangingLevel_ResetsSelectedDimensionToRealityAndLoadsItsDimensions()
    {
        var set = new ComplexResourceSetBlock { Name = "Set", PersistentId = 1 };
        var level1 = new LevelLayoutBlock { Name = "Level 1", PersistentId = 1, ComplexResourceSet = 1, ElevatorTile = "Elevator1.prefab" };
        var dim1 = new DimensionLayoutBlock { DimensionIndex = "Dimension_1", Name = "Level 1 Dim", ComplexResourceSet = 1, DimensionGeomorph = "DimGeo1.prefab" };
        level1.SetDimension(dim1);

        var level2 = new LevelLayoutBlock { Name = "Level 2", PersistentId = 2, ComplexResourceSet = 1, ElevatorTile = "Elevator2.prefab" };
        var dim2 = new DimensionLayoutBlock { DimensionIndex = "Dimension_1", Name = "Level 2 Dim", ComplexResourceSet = 1, DimensionGeomorph = "DimGeo2.prefab" };
        level2.SetDimension(dim2);

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level1, level2 });

        // On Level 1
        Assert.Equal(2, vm.AvailableDimensions.Count);
        Assert.Equal("Level 1 Dim", vm.AvailableDimensions[1].DisplayName);
        vm.SelectedDimension = vm.AvailableDimensions.First(d => d.DimensionIndex == "Dimension_1");
        Assert.Equal("Dimension_1", vm.ActiveDimensionIndex);

        // Switch to Level 2
        vm.SelectedLevel = level2;
        Assert.Equal("Reality", vm.SelectedDimension?.DimensionIndex);
        Assert.True(vm.IsViewingReality);
        Assert.Equal(2, vm.AvailableDimensions.Count);
        Assert.Equal("Level 2 Dim", vm.AvailableDimensions[1].DisplayName);
    }

    [Fact]
    public void LevelLayoutViewModel_CustomLevelName_UpdatesDisplayText()
    {
        var level = new LevelLayoutBlock { Name = "Initial Name", PersistentId = 5 };
        var vm = new LevelLayoutViewModel(null, new List<LevelLayoutBlock> { level });

        Assert.Equal("Initial Name", level.DisplayText);

        level.Name = "Underground Complex Alpha";
        Assert.Equal("Underground Complex Alpha", level.DisplayText);
        Assert.Contains("Underground Complex Alpha", vm.StatusText);
    }

    [Fact]
    public void LevelLayoutViewModel_DeleteDimensionConfirmation_Workflow()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Set",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem> { new PrefabItem { Prefab = "RealityElevator.prefab" } }
        };
        var level = new LevelLayoutBlock { Name = "Level", PersistentId = 1, ComplexResourceSet = 1, ElevatorTile = "RealityElevator.prefab" };
        var dim1 = new DimensionLayoutBlock { DimensionIndex = "Dimension_1", Name = "Laboratory Sector", ComplexResourceSet = 1, DimensionGeomorph = "Dim1Geo.prefab" };
        level.SetDimension(dim1);

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });
        vm.SelectedDimension = vm.AvailableDimensions.First(d => d.DimensionIndex == "Dimension_1");

        Assert.False(vm.IsConfirmingDeleteDimension);

        // Request delete opens confirmation
        vm.RequestDeleteActiveDimension();
        Assert.True(vm.IsConfirmingDeleteDimension);
        Assert.Contains("Laboratory Sector", vm.DimensionDeleteConfirmationMessage);

        // Cancel aborts deletion
        vm.CancelDeleteDimension();
        Assert.False(vm.IsConfirmingDeleteDimension);
        Assert.True(level.HasDimension("Dimension_1"));

        // Request again and confirm
        vm.RequestDeleteActiveDimension();
        Assert.True(vm.IsConfirmingDeleteDimension);
        bool confirmed = vm.ConfirmDeleteDimension();
        Assert.True(confirmed);
        Assert.False(vm.IsConfirmingDeleteDimension);
        Assert.False(level.HasDimension("Dimension_1"));
        Assert.True(vm.IsViewingReality);
    }

    [Fact]
    public void LevelLayoutViewModel_ActiveDimensionName_InlineEditing()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Set",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem> { new PrefabItem { Prefab = "RealityElevator.prefab" } }
        };
        var level = new LevelLayoutBlock { Name = "Main Level", PersistentId = 1, ComplexResourceSet = 1, ElevatorTile = "RealityElevator.prefab" };
        var dim1 = new DimensionLayoutBlock { DimensionIndex = "Dimension_1", Name = "Dim Alpha", ComplexResourceSet = 1, DimensionGeomorph = "Dim1Geo.prefab" };
        level.SetDimension(dim1);

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        // In Reality, name is fixed and inline edit is disabled
        Assert.True(vm.IsViewingReality);
        Assert.Equal("Reality", vm.ActiveDimensionName);
        Assert.False(vm.CanEditActiveDimensionName);

        // Switch to Dimension 1
        vm.SelectedDimension = vm.AvailableDimensions.First(d => d.DimensionIndex == "Dimension_1");
        Assert.True(vm.CanEditActiveDimensionName);
        Assert.Equal("Dim Alpha", vm.ActiveDimensionName);

        // Edit dimension name inline
        vm.ActiveDimensionName = "Research Complex";
        Assert.Equal("Research Complex", dim1.Name);
        Assert.Equal("Research Complex", vm.ActiveDimensionDisplayName);
        Assert.Equal("Dimension: Research Complex", vm.ActiveDimensionTitle);
        Assert.Equal("Research Complex", vm.AvailableDimensions.First(d => d.DimensionIndex == "Dimension_1").DisplayName);
    }

    [Fact]
    public void LevelLayoutViewModel_RealityDimension_CanOnlyHaveElevators()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Tech Complex",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/Elevators/Shaft1.prefab" },
                new PrefabItem { Prefab = "Assets/Elevators/Shaft2.prefab" }
            },
            CustomGeomorphs_Objective_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/Geomorphs/ObjectiveGeomorph1.prefab" }
            }
        };

        var level = new LevelLayoutBlock
        {
            Name = "Main Expedition",
            PersistentId = 1,
            ComplexResourceSet = 1,
            ElevatorTile = "Assets/Elevators/Shaft1.prefab"
        };

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        // Request edit for Reality dimension
        vm.RequestEditActiveDimension();
        Assert.True(vm.IsConfiguringDimension);
        Assert.True(vm.IsPromptingReality);
        Assert.Equal("Starting Elevator Tile:", vm.PromptStartingTileLabel);

        // Only Elevator Shafts source should be available for Reality
        Assert.Single(vm.AvailableTileSources);
        Assert.Equal("Elevator Shafts", vm.AvailableTileSources[0]);
        Assert.Equal("Elevator Shafts", vm.SelectedTileSource);

        // Attempting to select Custom Geomorphs on Reality should coerce back to Elevator Shafts
        vm.SelectedTileSource = "Dimension Geomorphs (Custom Objective 1x1)";
        Assert.Equal("Elevator Shafts", vm.SelectedTileSource);

        // Only elevator tiles are available to pick
        Assert.Equal(2, vm.AvailableDimensionTiles.Count);
        Assert.All(vm.AvailableDimensionTiles, t => Assert.Equal("Elevator Shaft", t.Category));

        // Select the second elevator shaft
        vm.SelectedDimensionTile = vm.AvailableDimensionTiles[1];
        bool confirmed = vm.ConfirmPromptDimension();
        Assert.True(confirmed);
        Assert.Equal("Assets/Elevators/Shaft2.prefab", level.ElevatorTile);
    }

    [Fact]
    public void LevelLayoutViewModel_OtherDimension_CanUseCustomGeomorphsOrElevators()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Tech Complex",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/Elevators/Shaft1.prefab" }
            },
            CustomGeomorphs_Objective_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/Geomorphs/ObjectiveGeomorph1.prefab" }
            }
        };

        var level = new LevelLayoutBlock
        {
            Name = "Main Expedition",
            PersistentId = 1,
            ComplexResourceSet = 1,
            ElevatorTile = "Assets/Elevators/Shaft1.prefab"
        };

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        // Add Dimension 1
        vm.OpenAddDimensionDialog();
        Assert.True(vm.IsConfiguringDimension);
        Assert.False(vm.IsPromptingReality);
        Assert.Equal("Starting Tile / DimensionGeomorph:", vm.PromptStartingTileLabel);

        // Non-reality dimensions have both sources available
        Assert.Equal(2, vm.AvailableTileSources.Count);
        Assert.Contains("Elevator Shafts", vm.AvailableTileSources);
        Assert.Contains("Dimension Geomorphs (Custom Objective 1x1)", vm.AvailableTileSources);

        // Choose Custom Geomorph
        vm.SelectedTileSource = "Dimension Geomorphs (Custom Objective 1x1)";
        Assert.Single(vm.AvailableDimensionTiles);
        Assert.Equal("Dimension Geomorph", vm.AvailableDimensionTiles[0].Category);
        Assert.Equal("Assets/Geomorphs/ObjectiveGeomorph1.prefab", vm.AvailableDimensionTiles[0].Prefab);

        vm.SelectedDimensionTile = vm.AvailableDimensionTiles[0];
        vm.ConfirmPromptDimension();

        var dim = level.GetDimension("Dimension_1");
        Assert.NotNull(dim);
        Assert.Equal("Assets/Geomorphs/ObjectiveGeomorph1.prefab", dim.DimensionGeomorph);
        Assert.Equal("Dimension Geomorph", dim.TileCategory);
    }

    [Fact]
    public void GeomorphMetadataService_LoadsVanillaMetadata_Successfully()
    {
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        var metadata = GeomorphMetadataService.GetMetadata(elevatorPrefab);

        Assert.NotNull(metadata);
        Assert.NotEmpty(metadata.AreaDatas);
        Assert.NotEmpty(metadata.AreaDatas[0].ExpanderDatas);

        // Verify the 4 expanders on this elevator shaft (Forwards, Backwards, Left, Right) and rotation
        var expanders = metadata.AreaDatas[0].ExpanderDatas;
        Assert.Contains(expanders, e => e.Direction == "Forwards" && e.ExpanderPosition.Z > 0 && Math.Round(e.YAngle, 1) == 0);
        Assert.Contains(expanders, e => e.Direction == "Backwards" && e.ExpanderPosition.Z < 0 && Math.Round(e.YAngle, 1) == 180);
        Assert.Contains(expanders, e => e.Direction == "Left" && e.ExpanderPosition.X < 0 && Math.Round(e.YAngle, 1) == 270);
        Assert.Contains(expanders, e => e.Direction == "Right" && e.ExpanderPosition.X > 0 && Math.Round(e.YAngle, 1) == 90);
    }

    [Fact]
    public void LevelLayoutGridState_InitializeStartingTile_PlacesAtCell00WithDoors()
    {
        var gridState = new LevelLayoutGridState();
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";

        gridState.InitializeStartingTile(elevatorPrefab, 0, 0);

        Assert.Single(gridState.PlacedGeomorphs);
        var geo = gridState.PlacedGeomorphs[0];
        Assert.Equal(0, geo.GridX);
        Assert.Equal(0, geo.GridY);
        Assert.Equal(0.0, geo.WorldLeft); // 0 * 64
        Assert.Equal(0.0, geo.WorldTop);  // 0 * 64
        Assert.Equal(32.0, geo.WorldX);   // 0 + 32
        Assert.Equal(32.0, geo.WorldY);   // 0 + 32
        Assert.Equal(64.0, geo.WidthInPixels);
        Assert.Equal(64.0, geo.HeightInPixels);

        // Doors
        Assert.Equal(4, gridState.PlacedDoors.Count);

        // Forwards door: Z = +32 -> mapped to LocalY = -32 -> WorldY = 32 - 32 = 0 (cell top edge), YAngle = 0
        var fwdDoor = gridState.PlacedDoors.FirstOrDefault(d => d.Direction == "Forwards");
        Assert.NotNull(fwdDoor);
        Assert.Equal(32.0, fwdDoor.WorldX, precision: 1);
        Assert.Equal(0.0, fwdDoor.WorldY, precision: 1);
        Assert.Equal(0.0, fwdDoor.RotationAngle, precision: 1);

        // Backwards door: Z = -32 -> mapped to LocalY = +32 -> WorldY = 32 + 32 = 64 (cell bottom edge), YAngle = 180
        var backDoor = gridState.PlacedDoors.FirstOrDefault(d => d.Direction == "Backwards");
        Assert.NotNull(backDoor);
        Assert.Equal(32.0, backDoor.WorldX, precision: 1);
        Assert.Equal(64.0, backDoor.WorldY, precision: 1);
        Assert.Equal(180.0, backDoor.RotationAngle, precision: 1);

        // Left door: X = -32 -> WorldX = 32 - 32 = 0 (cell left edge), YAngle = 270
        var leftDoor = gridState.PlacedDoors.FirstOrDefault(d => d.Direction == "Left");
        Assert.NotNull(leftDoor);
        Assert.Equal(0.0, leftDoor.WorldX, precision: 1);
        Assert.Equal(32.0, leftDoor.WorldY, precision: 1);
        Assert.Equal(270.0, leftDoor.RotationAngle, precision: 1);

        // Right door: X = +32 -> WorldX = 32 + 32 = 64 (cell right edge), YAngle = 90
        var rightDoor = gridState.PlacedDoors.FirstOrDefault(d => d.Direction == "Right");
        Assert.NotNull(rightDoor);
        Assert.Equal(64.0, rightDoor.WorldX, precision: 1);
        Assert.Equal(32.0, rightDoor.WorldY, precision: 1);
        Assert.Equal(90.0, rightDoor.RotationAngle, precision: 1);
    }

    [Fact]
    public void LevelLayoutGridState_PanAndZoom_BehaveCorrectly()
    {
        var gridState = new LevelLayoutGridState();
        gridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab", 0, 0);
        gridState.ResetView(800, 600);

        Assert.Equal(4.0, gridState.Zoom);
        Assert.Equal("400%", gridState.ZoomPercentage);
        // Center of cell (0,0) is (32, 32)
        // PanX = 800 / 2 - 32 * 4 = 400 - 128 = 272
        // PanY = 600 / 2 - 32 * 4 = 300 - 128 = 172
        Assert.Equal(272.0, gridState.PanX);
        Assert.Equal(172.0, gridState.PanY);

        gridState.Pan(50, -30);
        Assert.Equal(322.0, gridState.PanX);
        Assert.Equal(142.0, gridState.PanY);

        gridState.ZoomIn(400, 300);
        Assert.True(gridState.Zoom > 4.0);

        gridState.ZoomOut(400, 300);
        Assert.Equal(4.0, gridState.Zoom, precision: 2);
    }

    [Fact]
    public void LevelLayoutGridState_ZoomAt_ZoomsTowardCursor()
    {
        var gridState = new LevelLayoutGridState();
        gridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab", 0, 0);
        gridState.ResetView(800, 600); // Zoom = 4.0, PanX = 272, PanY = 172

        // Cursor at (400, 300) -> world position ((400 - 272)/4, (300 - 172)/4) = (32, 32)
        double mouseX = 400;
        double mouseY = 300;
        double worldXBefore = (mouseX - gridState.PanX) / gridState.Zoom;
        double worldYBefore = (mouseY - gridState.PanY) / gridState.Zoom;
        Assert.Equal(32.0, worldXBefore, precision: 2);
        Assert.Equal(32.0, worldYBefore, precision: 2);

        // Zoom in towards mouse cursor
        gridState.ZoomAt(1.0, mouseX, mouseY);
        Assert.True(gridState.Zoom > 4.0);

        // World position under cursor should remain exactly the same
        double worldXAfter = (mouseX - gridState.PanX) / gridState.Zoom;
        double worldYAfter = (mouseY - gridState.PanY) / gridState.Zoom;
        Assert.Equal(worldXBefore, worldXAfter, precision: 2);
        Assert.Equal(worldYBefore, worldYAfter, precision: 2);
    }

    [Fact]
    public void LevelLayoutGridState_ResetView_CentersOnPlacedElevatorTile()
    {
        var gridState = new LevelLayoutGridState();
        gridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab", 0, 0);

        Assert.Single(gridState.PlacedGeomorphs);
        var tile = gridState.PlacedGeomorphs[0];
        Assert.Equal(32.0, tile.WorldX);
        Assert.Equal(32.0, tile.WorldY);

        gridState.ResetView(500, 400);

        Assert.Equal(4.0, gridState.Zoom);
        // PanX = 500 / 2 - 32 * 4 = 250 - 128 = 122
        // PanY = 400 / 2 - 32 * 4 = 200 - 128 = 72
        Assert.Equal(122.0, gridState.PanX);
        Assert.Equal(72.0, gridState.PanY);

        // Screen position of tile center should be exactly at the center of the viewport (250, 200)
        double screenCenterX = tile.WorldX * gridState.Zoom + gridState.PanX;
        double screenCenterY = tile.WorldY * gridState.Zoom + gridState.PanY;
        Assert.Equal(250.0, screenCenterX);
        Assert.Equal(200.0, screenCenterY);
    }

    [Fact]
    public void PlacedDoorNode_HasProportionalDimensions()
    {
        var externalDoor = new PlacedDoorNode { AreaTo = -1 };
        Assert.False(externalDoor.IsInternal);
        Assert.Equal(4.0, externalDoor.DoorWidth);
        Assert.Equal(1.5, externalDoor.DoorHeight);

        var internalDoor = new PlacedDoorNode { AreaTo = 1 };
        Assert.True(internalDoor.IsInternal);
        Assert.Equal(4.0, internalDoor.DoorWidth);
        Assert.Equal(1.5, internalDoor.DoorHeight);
    }

    [Fact]
    public void LevelLayoutViewModel_GridState_UpdatesOnSelectionChanges()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Mining Complex",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab" }
            }
        };

        var level = new LevelLayoutBlock
        {
            Name = "Level A",
            PersistentId = 10,
            ComplexResourceSet = 1,
            ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab"
        };

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        Assert.NotNull(vm.GridState);
        Assert.Single(vm.GridState.PlacedGeomorphs);
        Assert.Equal(0, vm.GridState.PlacedGeomorphs[0].GridX);
        Assert.Equal(0, vm.GridState.PlacedGeomorphs[0].GridY);
        Assert.Equal(4, vm.GridState.PlacedDoors.Count);
    }

    [Fact]
    public void LevelLayoutGridState_ExpandFromDoor_PlacesGeomorphInCorrectDirection()
    {
        var gridState = new LevelLayoutGridState();
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        gridState.InitializeStartingTile(elevatorPrefab, 0, 0);

        var rightDoor = gridState.PlacedDoors.First(d => d.Direction == "Right");
        var (targetX, targetY, dirName) = gridState.GetExpansionTargetCell(rightDoor);

        Assert.Equal(1, targetX);
        Assert.Equal(0, targetY);
        Assert.Contains("East", dirName);

        // Expand with elevator prefab (which has all 4 external doors in room 0)
        var newGeo = gridState.ExpandFromDoor(rightDoor, elevatorPrefab, isSecurityDoor: false);

        Assert.NotNull(newGeo);
        Assert.Equal(2, gridState.PlacedGeomorphs.Count);
        Assert.Equal(1, newGeo.GridX);
        Assert.Equal(0, newGeo.GridY);
        Assert.True(rightDoor.IsConnected);
        Assert.NotNull(rightDoor.ConnectedDoor);
    }

    [Fact]
    public void LevelLayoutGridState_MultiRoomGeomorph_GeneratesOnlyFirstRoomInitially_AndProgressivelyAddsRooms()
    {
        var gridState = new LevelLayoutGridState();
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        gridState.InitializeStartingTile(elevatorPrefab, 0, 0);

        var rightDoor = gridState.PlacedDoors.First(d => d.Direction == "Right");
        string digSiteGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";

        var newGeo = gridState.ExpandFromDoor(rightDoor, digSiteGeo, isSecurityDoor: false);
        Assert.NotNull(newGeo);

        // Expanding East into digSiteGeo connects to the West (Left) door located in Area 2.
        // The first room (Area 2) is automatically generated/expanded!
        Assert.True(newGeo.IsRoomGenerated(2));
        Assert.False(newGeo.IsRoomGenerated(0));
        Assert.False(newGeo.IsRoomGenerated(1));
        Assert.False(newGeo.IsRoomGenerated(3));

        // The door expanded from is immediately connected and expanded
        Assert.True(rightDoor.IsConnected);
        Assert.NotNull(rightDoor.ConnectedDoor);
        Assert.Equal(rightDoor, rightDoor.ConnectedDoor.ConnectedDoor);

        // Area 2 has 3 doors: 1 external (Left door connected to elevator) and 2 internal room doors (to Area 0 and Area 1)
        Assert.Equal(3, newGeo.Doors.Count);
        var doorToArea0 = newGeo.Doors.First(d => d.IsInternal && (d.AreaFrom == 0 || d.AreaTo == 0));
        Assert.False(doorToArea0.IsConnected);

        // Now generate Area 0 from Area 2 door
        int generated = gridState.GenerateRoomFromDoor(doorToArea0);
        Assert.Equal(0, generated);
        Assert.True(newGeo.IsRoomGenerated(0));
        Assert.True(doorToArea0.IsConnected);
    }

    [Fact]
    public void LevelLayoutViewModel_OnDoorClicked_GeomorphDoor_OpensExpansionDialog()
    {
        var set = new ComplexResourceSetBlock
        {
            Name = "Mining Complex",
            PersistentId = 1,
            ElevatorShafts_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab" }
            },
            GeomorphTiles_1x1 = new List<PrefabItem>
            {
                new PrefabItem { Prefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab" }
            }
        };

        var level = new LevelLayoutBlock
        {
            Name = "Mining Level",
            PersistentId = 10,
            ComplexResourceSet = 1,
            ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab"
        };

        var vm = new LevelLayoutViewModel(new ObservableCollection<ComplexResourceSetBlock> { set }, new List<LevelLayoutBlock> { level });

        var forwardDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Forwards");
        Assert.False(forwardDoor.IsInternal);
        Assert.False(vm.IsPickingGeomorphForDoor);

        vm.OnDoorClicked(forwardDoor);

        Assert.True(vm.IsPickingGeomorphForDoor);
        Assert.Equal(forwardDoor, vm.SelectedDoorForExpansion);
        Assert.Equal("Cell (0, -1)", vm.ExpansionTargetCellText);
        Assert.NotEmpty(vm.AvailableExpansionGeomorphs);

        // Confirm expansion
        vm.SelectedExpansionGeomorph = vm.AvailableExpansionGeomorphs.First();
        bool success = vm.ConfirmDoorExpansion();

        Assert.True(success);
        Assert.False(vm.IsPickingGeomorphForDoor);
        Assert.Equal(2, vm.GridState.PlacedGeomorphs.Count);
        Assert.True(forwardDoor.IsConnected);
    }

    [Fact]
    public void LevelLayoutViewModel_OnDoorClicked_RoomDoor_PromptsRoomExpansionAndGeneratesRoom()
    {
        var vm = new LevelLayoutViewModel();
        string multiAreaGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        vm.GridState.InitializeStartingTile(multiAreaGeo, 0, 0);

        var internalDoor = vm.GridState.PlacedDoors.FirstOrDefault(d => d.IsInternal);
        Assert.NotNull(internalDoor);
        Assert.False(internalDoor.IsConnected);
        Assert.Contains("Click to expand", internalDoor.TooltipText);

        // Clicking ungenerated internal room door opens room expansion dialog
        vm.OnDoorClicked(internalDoor);

        Assert.True(vm.IsExpandingRoom);
        Assert.False(vm.IsPickingGeomorphForDoor);
        Assert.Equal(internalDoor, vm.SelectedRoomDoorForExpansion);
        Assert.False(vm.IsRoomExpansionSecurityDoor);

        // Confirm expansion as normal room door
        bool confirmed = vm.ConfirmRoomExpansion();
        Assert.True(confirmed);
        Assert.False(vm.IsExpandingRoom);
        Assert.True(internalDoor.IsConnected);
        Assert.False(internalDoor.IsSecurityDoor);
        Assert.Equal("#9D00FF", internalDoor.DoorBrush);
        Assert.Contains("Connected", internalDoor.TooltipText);

        // Clicking again on connected door stays connected and does not toggle
        vm.OnDoorClicked(internalDoor);
        Assert.False(internalDoor.IsSecurityDoor);
        Assert.Equal("#9D00FF", internalDoor.DoorBrush);
    }

    [Fact]
    public void LevelLayoutViewModel_OnDoorClicked_RoomDoor_CanExpandAsSecurityDoorAndStays()
    {
        var vm = new LevelLayoutViewModel();
        string multiAreaGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        vm.GridState.InitializeStartingTile(multiAreaGeo, 0, 0);

        var internalDoor = vm.GridState.PlacedDoors.FirstOrDefault(d => d.IsInternal && (d.AreaFrom == 1 || d.AreaTo == 1));
        Assert.NotNull(internalDoor);

        // Open prompt
        vm.OnDoorClicked(internalDoor);
        Assert.True(vm.IsExpandingRoom);

        // Set as Security Door
        vm.IsRoomExpansionSecurityDoor = true;
        bool confirmed = vm.ConfirmRoomExpansion();
        Assert.True(confirmed);
        Assert.False(vm.IsExpandingRoom);

        // Verify door is a security door
        Assert.True(internalDoor.IsConnected);
        Assert.True(internalDoor.IsSecurityDoor);
        Assert.Equal("#FF0000", internalDoor.DoorBrush);

        // Target area has its own zone
        var geo = internalDoor.Geomorph!;
        Assert.NotEqual(geo.GetRoomZone(0), geo.GetRoomZone(1));

        // Clicking the connected security door again does not toggle - it stays a security door
        vm.OnDoorClicked(internalDoor);
        Assert.True(internalDoor.IsSecurityDoor);
        Assert.Equal("#FF0000", internalDoor.DoorBrush);
    }

    [Fact]
    public void LevelLayoutViewModel_CancelRoomExpansion_DismissesDialogWithoutGenerating()
    {
        var vm = new LevelLayoutViewModel();
        string multiAreaGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        vm.GridState.InitializeStartingTile(multiAreaGeo, 0, 0);

        var internalDoor = vm.GridState.PlacedDoors.FirstOrDefault(d => d.IsInternal);
        Assert.NotNull(internalDoor);

        vm.OnDoorClicked(internalDoor);
        Assert.True(vm.IsExpandingRoom);

        vm.CancelRoomExpansion();
        Assert.False(vm.IsExpandingRoom);
        Assert.False(internalDoor.IsConnected);
    }

    [Fact]
    public void LevelLayoutGridState_MultiRoomGeomorph_GeneratesAllRoomsStepByStep()
    {
        var gridState = new LevelLayoutGridState();
        string multiAreaGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        var geoNode = gridState.CreateGeomorphNode(multiAreaGeo, 0, 0);
        gridState.PlacedGeomorphs.Add(geoNode);
        foreach (var door in geoNode.Doors)
        {
            gridState.PlacedDoors.Add(door);
        }

        // Room 0: 4 doors (1 external South + 3 room doors to Area 1, 2, 3)
        Assert.Equal(4, gridState.PlacedDoors.Count);
        Assert.Single(gridState.PlacedDoors.Where(d => !d.IsInternal)); // Backwards

        // Generate Area 1
        var doorToArea1 = gridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 1 || d.AreaTo == 1));
        gridState.GenerateRoomFromDoor(doorToArea1);
        Assert.True(geoNode.IsRoomGenerated(1));
        Assert.Equal(2, gridState.PlacedDoors.Count(d => !d.IsInternal)); // Backwards + Forwards

        // Generate Area 3
        var doorToArea3 = gridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 3 || d.AreaTo == 3));
        gridState.GenerateRoomFromDoor(doorToArea3);
        Assert.True(geoNode.IsRoomGenerated(3));
        Assert.Equal(3, gridState.PlacedDoors.Count(d => !d.IsInternal)); // Backwards + Forwards + Right

        // Generate Area 2
        var doorToArea2 = gridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 2 || d.AreaTo == 2));
        gridState.GenerateRoomFromDoor(doorToArea2);
        Assert.True(geoNode.IsRoomGenerated(2));
        Assert.Equal(4, gridState.PlacedDoors.Count(d => !d.IsInternal)); // Backwards + Forwards + Right + Left

        // All 4 areas generated
        Assert.True(geoNode.IsRoomGenerated(0));
        Assert.True(geoNode.IsRoomGenerated(1));
        Assert.True(geoNode.IsRoomGenerated(2));
        Assert.True(geoNode.IsRoomGenerated(3));

        // All room doors are now connected
        foreach (var door in gridState.PlacedDoors.Where(d => d.IsInternal))
        {
            Assert.True(door.IsConnected);
        }

        // Toggle security door on a room door
        gridState.ToggleSecurityDoor(doorToArea1);
        Assert.True(doorToArea1.IsSecurityDoor);
        Assert.Equal("#FF0000", doorToArea1.DoorBrush);
    }

    [Fact]
    public void LevelLayoutGridState_SecurityDoor_BlocksDuplicateRoutesBetweenSameAreas()
    {
        var gridState = new LevelLayoutGridState();
        var geoNode = new PlacedGeomorphNode
        {
            GridX = 0,
            GridY = 0
        };
        geoNode.UpdateWorldPosition();
        gridState.PlacedGeomorphs.Add(geoNode);

        // Create two room doors connecting Area 0 and Area 1 in the same geomorph
        var door1 = new PlacedDoorNode
        {
            Geomorph = geoNode,
            AreaFrom = 0,
            AreaTo = 1,
            Direction = "InZoneArea",
            LocalX = 0,
            LocalY = -10,
            WorldX = 32,
            WorldY = 22
        };

        var door2 = new PlacedDoorNode
        {
            Geomorph = geoNode,
            AreaFrom = 0,
            AreaTo = 1,
            Direction = "InZoneArea",
            LocalX = 0,
            LocalY = 10,
            WorldX = 32,
            WorldY = 42
        };

        geoNode.Doors.Add(door1);
        geoNode.Doors.Add(door2);
        gridState.PlacedDoors.Add(door1);
        gridState.PlacedDoors.Add(door2);

        gridState.RecalculateSecurityDoorConstraints();
        Assert.False(door1.IsBlocked);
        Assert.False(door2.IsBlocked);

        // Set door1 as a security door
        door1.IsSecurityDoor = true;
        gridState.RecalculateSecurityDoorConstraints();

        Assert.False(door1.IsBlocked);
        Assert.True(door2.IsBlocked); // door2 must be blocked / no longer exists

        // Unset security door
        door1.IsSecurityDoor = false;
        gridState.RecalculateSecurityDoorConstraints();

        Assert.False(door1.IsBlocked);
        Assert.False(door2.IsBlocked);
    }

    [Fact]
    public void LevelLayoutGridState_SecurityDoor_BlocksDuplicateRoutesBetweenSameGeomorphs()
    {
        var gridState = new LevelLayoutGridState();
        var geo1 = new PlacedGeomorphNode { GridX = 0, GridY = 0 };
        geo1.UpdateWorldPosition();
        var geo2 = new PlacedGeomorphNode { GridX = 1, GridY = 0 };
        geo2.UpdateWorldPosition();

        gridState.PlacedGeomorphs.Add(geo1);
        gridState.PlacedGeomorphs.Add(geo2);

        // Create two external door connections between geo1 and geo2
        var geo1DoorA = new PlacedDoorNode { Geomorph = geo1, AreaTo = -1, WorldX = 64, WorldY = 20 };
        var geo2DoorA = new PlacedDoorNode { Geomorph = geo2, AreaTo = -1, WorldX = 64, WorldY = 20 };
        geo1DoorA.ConnectedDoor = geo2DoorA;
        geo2DoorA.ConnectedDoor = geo1DoorA;

        var geo1DoorB = new PlacedDoorNode { Geomorph = geo1, AreaTo = -1, WorldX = 64, WorldY = 44 };
        var geo2DoorB = new PlacedDoorNode { Geomorph = geo2, AreaTo = -1, WorldX = 64, WorldY = 44 };
        geo1DoorB.ConnectedDoor = geo2DoorB;
        geo2DoorB.ConnectedDoor = geo1DoorB;

        gridState.PlacedDoors.Add(geo1DoorA);
        gridState.PlacedDoors.Add(geo2DoorA);
        gridState.PlacedDoors.Add(geo1DoorB);
        gridState.PlacedDoors.Add(geo2DoorB);

        gridState.RecalculateSecurityDoorConstraints();
        Assert.False(geo1DoorA.IsBlocked);
        Assert.False(geo1DoorB.IsBlocked);

        // Set Door A as security door
        geo1DoorA.IsSecurityDoor = true;
        geo2DoorA.IsSecurityDoor = true;
        gridState.RecalculateSecurityDoorConstraints();

        Assert.False(geo1DoorA.IsBlocked);
        Assert.False(geo2DoorA.IsBlocked);
        Assert.True(geo1DoorB.IsBlocked);
        Assert.True(geo2DoorB.IsBlocked);
    }

    [Fact]
    public void PlacedDoorNode_DoorBrush_ReflectsDoorTypesAndStates()
    {
        // Unexpanded doors (room door or geomorph door) are yellow
        var unexpandedRoomDoor = new PlacedDoorNode { AreaTo = 1, IsConnected = false };
        Assert.True(unexpandedRoomDoor.IsInternal);
        Assert.Equal("#FFFF00", unexpandedRoomDoor.DoorBrush); // Yellow

        var unexpandedGeoDoor = new PlacedDoorNode { AreaTo = -1, IsConnected = false };
        Assert.False(unexpandedGeoDoor.IsInternal);
        Assert.Equal("#FFFF00", unexpandedGeoDoor.DoorBrush); // Yellow

        // Connected standard doors are purple
        var connectedRoomDoor = new PlacedDoorNode { AreaTo = 1, IsConnected = true };
        Assert.Equal("#9D00FF", connectedRoomDoor.DoorBrush); // Purple

        var connectedGeoDoor = new PlacedDoorNode { AreaTo = -1, IsConnected = true };
        Assert.Equal("#9D00FF", connectedGeoDoor.DoorBrush); // Purple

        var securityDoor = new PlacedDoorNode { AreaTo = -1, IsConnected = true, IsSecurityDoor = true };
        Assert.Equal("#FF0000", securityDoor.DoorBrush); // Red

        var secondaryDoor = new PlacedDoorNode { AreaTo = -1, IsConnected = true, IsSecondaryDoor = true };
        Assert.Equal("#FFA200", secondaryDoor.DoorBrush); // Orange

        var overloadDoor = new PlacedDoorNode { AreaTo = -1, IsConnected = true, IsOverloadDoor = true };
        Assert.Equal("#00F2FF", overloadDoor.DoorBrush); // Cyan

        var blockedDoor = new PlacedDoorNode { AreaTo = 1, IsBlocked = true };
        Assert.Equal("#757575", blockedDoor.DoorBrush); // Grey
    }

    [Fact]
    public void LevelLayoutViewModel_ConfirmDoorExpansion_PreventsCollisionAndOutOfBounds()
    {
        var vm = new LevelLayoutViewModel();
        vm.GridState.InitializeStartingTile("Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab", 0, 0);

        var rightDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Right");
        vm.OpenDoorExpansionDialog(rightDoor);

        Assert.True(vm.IsPickingGeomorphForDoor);
        Assert.True(vm.ConfirmDoorExpansion());

        // Now cell (1, 0) is occupied. If we try to expand from another door into (1, 0):
        vm.OpenDoorExpansionDialog(rightDoor);
        Assert.False(vm.ConfirmDoorExpansion());
        Assert.True(vm.HasExpansionErrorMessage);
        Assert.Contains("occupied", vm.ExpansionErrorMessage);
    }

    [Fact]
    public void LevelLayoutGridState_MultiRoomGeomorph_SecurityDoorInLoop_BlocksAlternativePath()
    {
        var gridState = new LevelLayoutGridState();
        var geoNode = new PlacedGeomorphNode { GridX = 0, GridY = 0 };
        geoNode.UpdateWorldPosition();
        gridState.PlacedGeomorphs.Add(geoNode);

        // Generate rooms 0, 1, 2, 3 in the geomorph
        geoNode.GenerateRoom(0);
        geoNode.GenerateRoom(1);
        geoNode.GenerateRoom(2);
        geoNode.GenerateRoom(3);

        // Create a cycle of 4 doors: 0-1, 1-2, 2-3, 3-0
        var door01 = new PlacedDoorNode { Geomorph = geoNode, AreaFrom = 0, AreaTo = 1, Direction = "InZoneArea", WorldX = 20, WorldY = 20 };
        var door12 = new PlacedDoorNode { Geomorph = geoNode, AreaFrom = 1, AreaTo = 2, Direction = "InZoneArea", WorldX = 30, WorldY = 20 };
        var door23 = new PlacedDoorNode { Geomorph = geoNode, AreaFrom = 2, AreaTo = 3, Direction = "InZoneArea", WorldX = 30, WorldY = 30 };
        var door30 = new PlacedDoorNode { Geomorph = geoNode, AreaFrom = 3, AreaTo = 0, Direction = "InZoneArea", WorldX = 20, WorldY = 30 };

        geoNode.Doors.Add(door01);
        geoNode.Doors.Add(door12);
        geoNode.Doors.Add(door23);
        geoNode.Doors.Add(door30);

        gridState.PlacedDoors.Add(door01);
        gridState.PlacedDoors.Add(door12);
        gridState.PlacedDoors.Add(door23);
        gridState.PlacedDoors.Add(door30);

        // Initially no security doors -> all in Zone 0 and connected
        gridState.RecalculateSecurityDoorConstraints();
        Assert.Equal(0, geoNode.GetRoomZone(0));
        Assert.Equal(0, geoNode.GetRoomZone(1));
        Assert.Equal(0, geoNode.GetRoomZone(2));
        Assert.Equal(0, geoNode.GetRoomZone(3));
        Assert.False(door01.IsBlocked);
        Assert.False(door12.IsBlocked);
        Assert.False(door23.IsBlocked);
        Assert.False(door30.IsBlocked);

        // Now set door01 as a Security Door
        door01.IsSecurityDoor = true;
        gridState.RecalculateSecurityDoorConstraints();

        // Area 0 and 3 are in Zone 0 (connected via normal door 3-0)
        Assert.Equal(0, geoNode.GetRoomZone(0));
        Assert.Equal(0, geoNode.GetRoomZone(3));

        // Area 1 and 2 are in Zone 1 (connected via normal door 1-2)
        int zone1 = geoNode.GetRoomZone(1);
        Assert.NotEqual(0, zone1);
        Assert.Equal(zone1, geoNode.GetRoomZone(2));

        // door01 is the single security door connecting Zone 0 and Zone 1
        Assert.False(door01.IsBlocked);
        Assert.True(door01.IsConnected);

        // door12 (in Zone 1) and door30 (in Zone 0) are normal connected doors
        Assert.False(door12.IsBlocked);
        Assert.True(door12.IsConnected);
        Assert.False(door30.IsBlocked);
        Assert.True(door30.IsConnected);

        // door23 connects Area 2 (Zone 1) and Area 3 (Zone 0) without being a security door -> MUST BE BLOCKED!
        Assert.True(door23.IsBlocked);
        Assert.False(door23.IsConnected);

        // Unset security door -> everything becomes Zone 0 and door23 unblocks
        door01.IsSecurityDoor = false;
        gridState.RecalculateSecurityDoorConstraints();
        Assert.False(door23.IsBlocked);
        Assert.True(door23.IsConnected);
    }

    [Fact]
    public void LevelLayoutGridState_AdjacentGeomorphsInDifferentZones_NeverConnectDoors()
    {
        var gridState = new LevelLayoutGridState();
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        gridState.InitializeStartingTile(elevatorPrefab, 0, 0);

        var elevator = gridState.PlacedGeomorphs[0];
        Assert.Equal(0, elevator.GetRoomZone(0));

        // 1. Expand North with Security Door -> Geo North is in Zone 1
        var northDoor = gridState.PlacedDoors.First(d => d.Geomorph == elevator && d.Direction == "Forwards");
        var geoNorth = gridState.ExpandFromDoor(northDoor, elevatorPrefab, isSecurityDoor: true);
        Assert.NotNull(geoNorth);
        Assert.Equal(0, geoNorth.GridX);
        Assert.Equal(-1, geoNorth.GridY);
        Assert.NotEqual(0, geoNorth.GetRoomZone(0));
        int northZone = geoNorth.GetRoomZone(0);

        // 2. Expand East from Geo North (normal door) -> Geo NE is in Zone 1
        var geoNorthEastDoor = gridState.PlacedDoors.First(d => d.Geomorph == geoNorth && d.Direction == "Right");
        var geoNE = gridState.ExpandFromDoor(geoNorthEastDoor, elevatorPrefab, isSecurityDoor: false);
        Assert.NotNull(geoNE);
        Assert.Equal(1, geoNE.GridX);
        Assert.Equal(-1, geoNE.GridY);
        Assert.Equal(northZone, geoNE.GetRoomZone(0));

        // 3. Expand East from Elevator (normal door) -> Geo East is in Zone 0
        var elevatorEastDoor = gridState.PlacedDoors.First(d => d.Geomorph == elevator && d.Direction == "Right");
        var geoEast = gridState.ExpandFromDoor(elevatorEastDoor, elevatorPrefab, isSecurityDoor: false);
        Assert.NotNull(geoEast);
        Assert.Equal(1, geoEast.GridX);
        Assert.Equal(0, geoEast.GridY);
        Assert.Equal(0, geoEast.GetRoomZone(0));

        // Now Geo NE (1, -1 in Zone 1) and Geo East (1, 0 in Zone 0) are adjacent!
        // Geo NE's South door (Backwards) and Geo East's North door (Forwards) face each other
        var geoNESouthDoor = gridState.PlacedDoors.First(d => d.Geomorph == geoNE && d.Direction == "Backwards");
        var geoEastNorthDoor = gridState.PlacedDoors.First(d => d.Geomorph == geoEast && d.Direction == "Forwards");

        // Since they are in different zones and not connected by a security door, they must NOT connect and MUST be blocked!
        Assert.False(geoNESouthDoor.IsConnected);
        Assert.False(geoEastNorthDoor.IsConnected);
        Assert.True(geoNESouthDoor.IsBlocked);
        Assert.True(geoEastNorthDoor.IsBlocked);
    }

    [Fact]
    public void LevelLayoutGridState_NewRoomInheritsZone_AndDoorsLeadingToAnotherZoneAreBlocked()
    {
        var gridState = new LevelLayoutGridState();
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        gridState.InitializeStartingTile(elevatorPrefab, 0, 0);

        var elevator = gridState.PlacedGeomorphs[0];
        var eastDoor = gridState.PlacedDoors.First(d => d.Direction == "Right");

        // Expand East as a Security Door with multi-room digsite geo
        string digSiteGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        var geoDigSite = gridState.ExpandFromDoor(eastDoor, digSiteGeo, isSecurityDoor: true);
        Assert.NotNull(geoDigSite);

        // When expanding East, the entrance room is Area 2 (West door), placed in Zone 1 (Security Door)
        Assert.True(geoDigSite.IsRoomGenerated(2));
        Assert.Equal(1, geoDigSite.GetRoomZone(2));

        // The West door on Area 2 connects to the elevator East door as the primary Security Door between Zone 0 and Zone 1
        var westDoor = geoDigSite.Doors.First(d => !d.IsInternal && d.Direction == "Left");
        Assert.True(westDoor.IsConnected);
        Assert.True(westDoor.IsSecurityDoor);
        Assert.False(westDoor.IsBlocked);

        // Now generate Area 0 from Area 2
        var doorToArea0 = geoDigSite.Doors.First(d => d.IsInternal && (d.AreaFrom == 0 || d.AreaTo == 0));
        gridState.GenerateRoomFromDoor(doorToArea0);

        // Area 0 inherits the zone of Area 2
        Assert.Equal(geoDigSite.GetRoomZone(2), geoDigSite.GetRoomZone(0));

        // Now generate Area 1 from Area 0
        var doorToArea1 = geoDigSite.Doors.First(d => d.IsInternal && (d.AreaFrom == 1 || d.AreaTo == 1));
        gridState.GenerateRoomFromDoor(doorToArea1);

        // Area 1 inherits the zone of Area 0
        Assert.Equal(geoDigSite.GetRoomZone(0), geoDigSite.GetRoomZone(1));
    }

    [Fact]
    public void LevelLayoutViewModel_OnDoorClicked_BlockedDoor_DoesNotExpandOrGenerate()
    {
        var vm = new LevelLayoutViewModel();
        var blockedDoor = new PlacedDoorNode
        {
            AreaFrom = 0,
            AreaTo = -1,
            IsBlocked = true
        };

        vm.OnDoorClicked(blockedDoor);
        Assert.False(vm.IsPickingGeomorphForDoor);
        Assert.Contains("blocked", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LevelLayoutGridState_UngeneratedInternalDoors_InNonZeroZone_AreNotBlocked()
    {
        var vm = new LevelLayoutViewModel();
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        vm.GridState.InitializeStartingTile(elevatorPrefab, 0, 0);

        var elevator = vm.GridState.PlacedGeomorphs[0];
        var eastDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Right");

        // Expand East across a Security Door (places geomorph in Zone 1, entrance is Area 2)
        string digSiteGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        var geoDigSite = vm.GridState.ExpandFromDoor(eastDoor, digSiteGeo, isSecurityDoor: true);
        Assert.NotNull(geoDigSite);
        Assert.NotEqual(0, geoDigSite.GetRoomZone(2));

        // Verify that internal doors on geoDigSite leading to ungenerated areas (0, 1) are NOT blocked
        var ungeneratedInternalDoors = vm.GridState.PlacedDoors
            .Where(d => d.Geomorph == geoDigSite && d.IsInternal)
            .ToList();
        Assert.NotEmpty(ungeneratedInternalDoors);

        foreach (var door in ungeneratedInternalDoors)
        {
            Assert.False(door.IsBlocked, $"Internal door [Area {door.AreaFrom} <-> Area {door.AreaTo}] should not be blocked before generation.");
            Assert.False(door.IsConnected);

            // Clicking should trigger room expansion
            vm.OnDoorClicked(door);
            Assert.True(vm.IsExpandingRoom);
            vm.CancelRoomExpansion();
        }

        // Generate Area 0 from Area 2
        var doorToArea0 = ungeneratedInternalDoors.First(d => d.AreaFrom == 0 || d.AreaTo == 0);
        vm.OnDoorClicked(doorToArea0);
        Assert.True(vm.IsExpandingRoom);
        bool confirmed = vm.ConfirmRoomExpansion();
        Assert.True(confirmed);
        Assert.True(geoDigSite.IsRoomGenerated(0));

        // Verify remaining ungenerated internal doors (Areas 1, 3) are STILL not blocked
        var remainingUngenerated = vm.GridState.PlacedDoors
            .Where(d => d.Geomorph == geoDigSite && d.IsInternal && (!geoDigSite.IsRoomGenerated(d.AreaFrom) || !geoDigSite.IsRoomGenerated(d.AreaTo)))
            .ToList();
        Assert.NotEmpty(remainingUngenerated);
        foreach (var door in remainingUngenerated)
        {
            Assert.False(door.IsBlocked, $"Internal door [Area {door.AreaFrom} <-> Area {door.AreaTo}] should remain unblocked.");
        }
    }

    [Fact]
    public void LevelLayoutGridState_GridSize_AndShrinkWarning_WorkCorrectly()
    {
        var gridState = new LevelLayoutGridState();
        gridState.GridSize = 40;
        gridState.CellDimension = 64.0;

        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        gridState.InitializeStartingTile(elevatorPrefab, 0, 0);

        Assert.False(gridState.HasGridSizeWarning);

        // Expand East 3 times -> placing geos at (1, 0), (2, 0), (3, 0)
        var door0 = gridState.PlacedDoors.First(d => d.Geomorph.GridX == 0 && d.Direction == "Right");
        var geo1 = gridState.ExpandFromDoor(door0, elevatorPrefab, false);
        Assert.NotNull(geo1);

        var door1 = gridState.PlacedDoors.First(d => d.Geomorph.GridX == 1 && d.Direction == "Right");
        var geo2 = gridState.ExpandFromDoor(door1, elevatorPrefab, false);
        Assert.NotNull(geo2);

        var door2 = gridState.PlacedDoors.First(d => d.Geomorph.GridX == 2 && d.Direction == "Right");
        var geo3 = gridState.ExpandFromDoor(door2, elevatorPrefab, false);
        Assert.NotNull(geo3);

        // Total layout width is 4 cells (from minX=0 to maxX=3)
        var bounds = gridState.GetLayoutBounds();
        Assert.Equal(4, bounds.width);
        Assert.Equal(1, bounds.height);
        Assert.False(gridState.HasGridSizeWarning);

        // Shrink grid size to 3 (which is less than layout width of 4)
        gridState.GridSize = 3;
        Assert.True(gridState.HasGridSizeWarning);
        Assert.Contains("exceed the grid size", gridState.GridSizeWarningMessage);

        // Expand grid size back to 10
        gridState.GridSize = 10;
        Assert.False(gridState.HasGridSizeWarning);
    }

    [Fact]
    public void ComplexResourceSetsViewModel_GridSizeWarning_NotifiesWhenGridSizeIsInvalid()
    {
        var block = new ComplexResourceSetBlock
        {
            Name = "Mining Block",
            PersistentId = 1,
            LevelGenConfig = new LevelGenConfig
            {
                GridSize = 40,
                CellDimension = 64
            }
        };

        var vm = new ComplexResourceSetsViewModel(new List<ComplexResourceSetBlock> { block });
        vm.SelectedBlock = block;

        Assert.False(vm.HasGridSizeWarning);

        block.LevelGenConfig.GridSize = 0;
        Assert.True(vm.HasGridSizeWarning);
        Assert.Contains("at least 1", vm.GridSizeWarningMessage);

        block.LevelGenConfig.GridSize = 30;
        Assert.False(vm.HasGridSizeWarning);
    }

    [Fact]
    public void LevelLayoutViewModel_DoorOptions_CanExpandAsSecondaryAndOverloadDoor_AndEnforcesLimit()
    {
        var vm = new LevelLayoutViewModel();
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        vm.GridState.InitializeStartingTile(elevatorPrefab, 0, 0);

        Assert.True(vm.CanAddSecondaryDoor);
        Assert.True(vm.CanAddOverloadDoor);
        Assert.False(vm.GridState.HasSecondaryDoor);
        Assert.False(vm.GridState.HasOverloadDoor);

        var rightDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Right");
        vm.OpenDoorExpansionDialog(rightDoor);

        Assert.True(vm.IsPickingGeomorphForDoor);
        Assert.True(vm.CanAddSecondaryDoor);
        Assert.True(vm.CanAddOverloadDoor);
        Assert.True(vm.IsExpansionRoomDoor);

        // Select Secondary Door
        vm.IsExpansionSecondaryDoor = true;
        Assert.Equal(DoorType.SecondaryDoor, vm.SelectedExpansionDoorType);
        bool confirmed = vm.ConfirmDoorExpansion();
        Assert.True(confirmed);

        Assert.True(rightDoor.IsSecondaryDoor);
        Assert.Equal("#FFA200", rightDoor.DoorBrush);
        Assert.Contains("[Secondary Door]", rightDoor.TooltipText);
        Assert.True(vm.GridState.HasSecondaryDoor);
        Assert.False(vm.CanAddSecondaryDoor);
        Assert.True(vm.CanAddOverloadDoor);

        // Try expanding South door as another Secondary Door
        var southDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Backwards" && !d.IsConnected);
        vm.OpenDoorExpansionDialog(southDoor);
        Assert.False(vm.CanAddSecondaryDoor);
        Assert.True(vm.CanAddOverloadDoor);

        // Attempting to force secondary door should fail validation
        vm.SelectedExpansionDoorType = DoorType.SecondaryDoor;
        bool failConfirm = vm.ConfirmDoorExpansion();
        Assert.False(failConfirm);
        Assert.Contains("Secondary Door has already been added", vm.ExpansionErrorMessage);

        // Now select Overload Door instead
        vm.IsExpansionOverloadDoor = true;
        Assert.Equal(DoorType.OverloadDoor, vm.SelectedExpansionDoorType);
        bool overloadConfirmed = vm.ConfirmDoorExpansion();
        Assert.True(overloadConfirmed);

        Assert.True(southDoor.IsOverloadDoor);
        Assert.Equal("#00F2FF", southDoor.DoorBrush);
        Assert.Contains("[Overload Door]", southDoor.TooltipText);
        Assert.True(vm.GridState.HasOverloadDoor);
        Assert.False(vm.CanAddOverloadDoor);
        Assert.False(vm.CanAddSecondaryDoor);
    }

    [Fact]
    public void LevelLayoutViewModel_RoomExpansion_CanExpandAsSecondaryAndOverloadDoor_AndEnforcesLimit()
    {
        var vm = new LevelLayoutViewModel();
        string multiAreaGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        vm.GridState.InitializeStartingTile(multiAreaGeo, 0, 0);

        var internalDoor1 = vm.GridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 1 || d.AreaTo == 1));
        var internalDoor2 = vm.GridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 2 || d.AreaTo == 2));

        Assert.True(vm.CanAddSecondaryDoor);
        Assert.True(vm.CanAddOverloadDoor);

        // Expand Room 1 as Secondary Door
        vm.OpenRoomExpansionDialog(internalDoor1);
        Assert.True(vm.CanAddSecondaryDoor);
        Assert.True(vm.CanAddOverloadDoor);
        Assert.True(vm.IsRoomExpansionRoomDoor);

        vm.IsRoomExpansionSecondaryDoor = true;
        Assert.Equal(DoorType.SecondaryDoor, vm.SelectedRoomExpansionDoorType);
        bool confirmed1 = vm.ConfirmRoomExpansion();
        Assert.True(confirmed1);

        Assert.True(internalDoor1.IsSecondaryDoor);
        Assert.Equal("#FFA200", internalDoor1.DoorBrush);
        Assert.True(vm.GridState.HasSecondaryDoor);
        Assert.False(vm.CanAddSecondaryDoor);

        // Try expanding Room 2 as Secondary Door -> fails
        vm.OpenRoomExpansionDialog(internalDoor2);
        Assert.False(vm.CanAddSecondaryDoor);
        vm.SelectedRoomExpansionDoorType = DoorType.SecondaryDoor;
        bool failedConfirm = vm.ConfirmRoomExpansion();
        Assert.False(failedConfirm);
        Assert.Contains("Secondary Door has already been added", vm.RoomExpansionErrorMessage);

        // Select Overload Door instead -> succeeds
        vm.IsRoomExpansionOverloadDoor = true;
        bool confirmed2 = vm.ConfirmRoomExpansion();
        Assert.True(confirmed2);

        Assert.True(internalDoor2.IsOverloadDoor);
        Assert.Equal("#00F2FF", internalDoor2.DoorBrush);
        Assert.True(vm.GridState.HasOverloadDoor);
        Assert.False(vm.CanAddOverloadDoor);
    }

    [Fact]
    public void PlacedDoorNode_GeomorphAndRoomDoors_LookIdenticalAndColorByExpansionState()
    {
        var vm = new LevelLayoutViewModel();
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        string multiAreaGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        vm.GridState.InitializeStartingTile(elevatorPrefab, 0, 0);

        var rightDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Right" && !d.IsConnected);
        Assert.False(rightDoor.IsInternal);
        Assert.Equal("#FFFF00", rightDoor.DoorBrush); // Yellow when unexpanded

        // Expand elevator right door with another elevator geomorph (1x1 with 4 external doors in Room 0)
        var placedGeo = vm.GridState.ExpandFromDoor(rightDoor, elevatorPrefab, DoorType.RoomDoor);
        Assert.NotNull(placedGeo);

        // Expanded geomorph door is now connected -> purple
        Assert.True(rightDoor.IsConnected);
        Assert.Equal("#9D00FF", rightDoor.DoorBrush);

        // Now expand from the new geomorph's east door into a multi-room geomorph
        var newGeoEastDoor = placedGeo.Doors.First(d => d.Direction == "Right" && !d.IsConnected);
        var multiGeo = vm.GridState.ExpandFromDoor(newGeoEastDoor, multiAreaGeo, DoorType.RoomDoor);
        Assert.NotNull(multiGeo);

        // Find an unexpanded internal room door inside the multi-room geomorph
        var internalRoomDoor = vm.GridState.PlacedDoors.First(d => d.IsInternal && !d.IsConnected);
        Assert.True(internalRoomDoor.IsInternal);

        // Unexpanded room door looks identical to unexpanded geomorph door: same dimensions (4.0 x 1.5) and yellow (#FFFF00)
        Assert.Equal(rightDoor.DoorWidth, internalRoomDoor.DoorWidth);
        Assert.Equal(rightDoor.DoorHeight, internalRoomDoor.DoorHeight);
        Assert.Equal("#FFFF00", internalRoomDoor.DoorBrush);

        // Expand the room door
        vm.OpenRoomExpansionDialog(internalRoomDoor);
        bool expandedRoom = vm.ConfirmRoomExpansion();
        Assert.True(expandedRoom);

        // Once expanded/connected, the room door turns purple (#9D00FF) and still has identical dimensions
        Assert.True(internalRoomDoor.IsConnected);
        Assert.Equal("#9D00FF", internalRoomDoor.DoorBrush);
        Assert.Equal(rightDoor.DoorWidth, internalRoomDoor.DoorWidth);
        Assert.Equal(rightDoor.DoorHeight, internalRoomDoor.DoorHeight);

        // Backend functionality remains differentiated
        Assert.True(internalRoomDoor.IsInternal);
        Assert.False(rightDoor.IsInternal);
    }

    [Fact]
    public void Doors_WhenRoomGeneratedOnBothSides_SameZone_MarkedAsExpanded_EvenIfNotDirectlyExpandedDoor()
    {
        var vm = new LevelLayoutViewModel();
        string multiAreaGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        vm.GridState.InitializeStartingTile(multiAreaGeo, 0, 0);

        var geo = vm.GridState.PlacedGeomorphs[0];
        Assert.True(geo.IsRoomGenerated(0));
        Assert.False(geo.IsRoomGenerated(1));
        Assert.False(geo.IsRoomGenerated(2));

        var door0To1 = vm.GridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 0 && d.AreaTo == 1 || d.AreaFrom == 1 && d.AreaTo == 0));
        var door0To2 = vm.GridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 0 && d.AreaTo == 2 || d.AreaFrom == 2 && d.AreaTo == 0));

        // Initially both doors are unexpanded (yellow)
        Assert.False(door0To1.IsConnected);
        Assert.False(door0To1.IsBlocked);
        Assert.Equal("#FFFF00", door0To1.DoorBrush);

        Assert.False(door0To2.IsConnected);
        Assert.False(door0To2.IsBlocked);
        Assert.Equal("#FFFF00", door0To2.DoorBrush);

        // Step 1: Expand Room 1 from door0To1 as a normal Room Door
        vm.OpenRoomExpansionDialog(door0To1);
        bool expanded1 = vm.ConfirmRoomExpansion();
        Assert.True(expanded1);
        Assert.True(geo.IsRoomGenerated(1));

        // door0To1 is now connected (purple)
        Assert.True(door0To1.IsConnected);
        Assert.False(door0To1.IsBlocked);
        Assert.Equal("#9D00FF", door0To1.DoorBrush);

        // door0To2 is still unexpanded (Area 2 is not generated yet)
        Assert.False(door0To2.IsConnected);
        Assert.False(door0To2.IsBlocked);
        Assert.Equal("#FFFF00", door0To2.DoorBrush);

        // Find internal door between Area 1 and Area 2
        var door1To2 = vm.GridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 1 && d.AreaTo == 2 || d.AreaFrom == 2 && d.AreaTo == 1));
        Assert.False(door1To2.IsConnected);
        Assert.False(door1To2.IsBlocked);
        Assert.Equal("#FFFF00", door1To2.DoorBrush);

        // Step 2: Expand Room 2 from door1To2 as a normal Room Door (NOT door0To2)
        vm.OpenRoomExpansionDialog(door1To2);
        bool expanded2 = vm.ConfirmRoomExpansion();
        Assert.True(expanded2);
        Assert.True(geo.IsRoomGenerated(2));

        // door1To2 is connected (purple)
        Assert.True(door1To2.IsConnected);
        Assert.False(door1To2.IsBlocked);
        Assert.Equal("#9D00FF", door1To2.DoorBrush);

        // All 3 rooms are in the same zone (Zone 0)
        Assert.Equal(0, geo.GetRoomZone(0));
        Assert.Equal(0, geo.GetRoomZone(1));
        Assert.Equal(0, geo.GetRoomZone(2));

        // Crucial requirement check: door0To2 was NOT the door that was expanded,
        // but now rooms on both sides (Area 0 and Area 2) have been generated in the same zone.
        // It must now be marked as expanded (connected, purple)!
        Assert.True(door0To2.IsConnected);
        Assert.False(door0To2.IsBlocked);
        Assert.Equal("#9D00FF", door0To2.DoorBrush);
    }

    [Fact]
    public void Doors_WhenRoomGeneratedOnBothSides_DifferentZones_MarkedAsBlocked_AndSecondaryOverloadAreZoneSeparators()
    {
        var vm = new LevelLayoutViewModel();
        string multiAreaGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        vm.GridState.InitializeStartingTile(multiAreaGeo, 0, 0);

        var geo = vm.GridState.PlacedGeomorphs[0];
        var door0To1 = vm.GridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 0 && d.AreaTo == 1 || d.AreaFrom == 1 && d.AreaTo == 0));
        var door0To2 = vm.GridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 0 && d.AreaTo == 2 || d.AreaFrom == 2 && d.AreaTo == 0));

        // Expand Room 1 from door0To1 as a Secondary Door (Zone separator)
        vm.OpenRoomExpansionDialog(door0To1);
        vm.IsRoomExpansionSecondaryDoor = true;
        bool expanded1 = vm.ConfirmRoomExpansion();
        Assert.True(expanded1);

        // Area 0 is in Zone 0, Area 1 is in Zone 1 (Secondary Door separated zones)
        Assert.Equal(0, geo.GetRoomZone(0));
        Assert.NotEqual(0, geo.GetRoomZone(1));
        int secondaryZone = geo.GetRoomZone(1);

        Assert.True(door0To1.IsConnected);
        Assert.True(door0To1.IsSecondaryDoor);
        Assert.Equal("#FFA200", door0To1.DoorBrush);

        // Find internal door between Area 1 and Area 2, expand as standard Room Door
        var door1To2 = vm.GridState.PlacedDoors.First(d => d.IsInternal && (d.AreaFrom == 1 && d.AreaTo == 2 || d.AreaFrom == 2 && d.AreaTo == 1));
        vm.OpenRoomExpansionDialog(door1To2);
        bool expanded2 = vm.ConfirmRoomExpansion();
        Assert.True(expanded2);

        // Area 2 is in the same zone as Area 1 (Zone 1)
        Assert.Equal(secondaryZone, geo.GetRoomZone(2));

        // door1To2 is connected within Zone 1
        Assert.True(door1To2.IsConnected);
        Assert.Equal("#9D00FF", door1To2.DoorBrush);

        // Crucial requirement check: door0To2 has Area 0 (Zone 0) on one side and Area 2 (Zone 1) on the other.
        // Since both sides are generated and they are in different zones without being the designated zone separator,
        // door0To2 MUST be marked as BLOCKED (grey, not connected)!
        Assert.False(door0To2.IsConnected);
        Assert.True(door0To2.IsBlocked);
        Assert.Equal("#757575", door0To2.DoorBrush);
        Assert.Contains("Blocked by Zone", door0To2.TooltipText);
    }

    [Fact]
    public void Doors_ExternalFacingDoors_ConnectedWhenSameZone_AndBlockedWhenDifferentZones()
    {
        var vm = new LevelLayoutViewModel();
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        vm.GridState.InitializeStartingTile(elevatorPrefab, 0, 0);

        // Create a 2x2 loop of 1x1 geomorphs:
        // (0,0) [Elevator, Zone 0] -> expands East to (1,0) [Zone 0]
        // (1,0) -> expands North to (1,-1) [Overload Door -> Zone 1]
        // (1,-1) -> expands West to (0,-1) [Zone 1]
        // Now (0,-1) [Zone 1] faces (0,0) [Zone 0] on South/North boundary!

        // 1. Expand East from (0,0) as Room Door
        var eastDoor00 = vm.GridState.PlacedDoors.First(d => d.Geomorph.GridX == 0 && d.Geomorph.GridY == 0 && d.Direction == "Right");
        var geo10 = vm.GridState.ExpandFromDoor(eastDoor00, elevatorPrefab, DoorType.RoomDoor);
        Assert.NotNull(geo10);
        Assert.Equal(0, geo10.GetRoomZone(0));

        // 2. Expand North from (1,0) as Overload Door (Zone separator)
        var northDoor10 = vm.GridState.PlacedDoors.First(d => d.Geomorph.GridX == 1 && d.Geomorph.GridY == 0 && d.Direction == "Forwards");
        var geo1neg1 = vm.GridState.ExpandFromDoor(northDoor10, elevatorPrefab, DoorType.OverloadDoor);
        Assert.NotNull(geo1neg1);
        int overloadZone = geo1neg1.GetRoomZone(0);
        Assert.NotEqual(0, overloadZone);
        Assert.True(northDoor10.IsOverloadDoor);

        // 3. Expand West from (1,-1) as Room Door
        var westDoor1neg1 = vm.GridState.PlacedDoors.First(d => d.Geomorph.GridX == 1 && d.Geomorph.GridY == -1 && d.Direction == "Left");
        var geo0neg1 = vm.GridState.ExpandFromDoor(westDoor1neg1, elevatorPrefab, DoorType.RoomDoor);
        Assert.NotNull(geo0neg1);
        Assert.Equal(overloadZone, geo0neg1.GetRoomZone(0));

        // Now (0,-1) [Zone 1] has a South (Backwards) door facing (0,0) [Zone 0]'s North (Forwards) door!
        var southDoor0neg1 = vm.GridState.PlacedDoors.First(d => d.Geomorph.GridX == 0 && d.Geomorph.GridY == -1 && d.Direction == "Backwards");
        var northDoor00 = vm.GridState.PlacedDoors.First(d => d.Geomorph.GridX == 0 && d.Geomorph.GridY == 0 && d.Direction == "Forwards");

        // Both sides are generated (Room 0 on (0,-1) is generated, Room 0 on (0,0) is generated).
        // Since (0,-1) is in Zone 1 (Overload) and (0,0) is in Zone 0, and this doorway is NOT the designated primary overload door:
        // Both doors must be marked as BLOCKED!
        Assert.False(southDoor0neg1.IsConnected);
        Assert.True(southDoor0neg1.IsBlocked);
        Assert.Equal("#757575", southDoor0neg1.DoorBrush);

        Assert.False(northDoor00.IsConnected);
        Assert.True(northDoor00.IsBlocked);
        Assert.Equal("#757575", northDoor00.DoorBrush);
    }

    [Fact]
    public void GeomorphExpansion_AutomaticallyExpandsConnectingEntranceRoom_AndMarksSourceDoorAsExpanded_InAllDirections()
    {
        string elevatorPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_04.prefab";
        string multiAreaGeo = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";

        // In digSiteGeo:
        // Area 0 contains Backwards (-Z / South) door
        // Area 1 contains Forwards (+Z / North) door
        // Area 2 contains Left (-X / West) door
        // Area 3 contains Right (+X / East) door

        // 1. Expand North from elevator:
        // Target is (0, -1). Facing door on digSiteGeo is Backwards (South) door -> located in Area 0!
        {
            var vm = new LevelLayoutViewModel();
            vm.GridState.InitializeStartingTile(elevatorPrefab, 0, 0);
            var northDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Forwards");

            var newGeo = vm.GridState.ExpandFromDoor(northDoor, multiAreaGeo, DoorType.RoomDoor);
            Assert.NotNull(newGeo);

            Assert.True(northDoor.IsConnected);
            Assert.Equal("#9D00FF", northDoor.DoorBrush);
            Assert.True(newGeo.IsRoomGenerated(0));
            Assert.False(newGeo.IsRoomGenerated(1));
            Assert.False(newGeo.IsRoomGenerated(2));
            Assert.False(newGeo.IsRoomGenerated(3));
        }

        // 2. Expand South from elevator:
        // Target is (0, 1). Facing door on digSiteGeo is Forwards (North) door -> located in Area 1!
        {
            var vm = new LevelLayoutViewModel();
            vm.GridState.InitializeStartingTile(elevatorPrefab, 0, 0);
            var southDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Backwards");

            var newGeo = vm.GridState.ExpandFromDoor(southDoor, multiAreaGeo, DoorType.RoomDoor);
            Assert.NotNull(newGeo);

            Assert.True(southDoor.IsConnected);
            Assert.Equal("#9D00FF", southDoor.DoorBrush);
            Assert.True(newGeo.IsRoomGenerated(1)); // Area 1 automatically generated!
            Assert.False(newGeo.IsRoomGenerated(0));
            Assert.False(newGeo.IsRoomGenerated(2));
            Assert.False(newGeo.IsRoomGenerated(3));
        }

        // 3. Expand East from elevator:
        // Target is (1, 0). Facing door on digSiteGeo is Left (West) door -> located in Area 2!
        {
            var vm = new LevelLayoutViewModel();
            vm.GridState.InitializeStartingTile(elevatorPrefab, 0, 0);
            var eastDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Right");

            var newGeo = vm.GridState.ExpandFromDoor(eastDoor, multiAreaGeo, DoorType.RoomDoor);
            Assert.NotNull(newGeo);

            Assert.True(eastDoor.IsConnected);
            Assert.Equal("#9D00FF", eastDoor.DoorBrush);
            Assert.True(newGeo.IsRoomGenerated(2)); // Area 2 automatically generated!
            Assert.False(newGeo.IsRoomGenerated(0));
            Assert.False(newGeo.IsRoomGenerated(1));
            Assert.False(newGeo.IsRoomGenerated(3));
        }

        // 4. Expand West from elevator:
        // Target is (-1, 0). Facing door on digSiteGeo is Right (East) door -> located in Area 3!
        {
            var vm = new LevelLayoutViewModel();
            vm.GridState.InitializeStartingTile(elevatorPrefab, 0, 0);
            var westDoor = vm.GridState.PlacedDoors.First(d => d.Direction == "Left");

            var newGeo = vm.GridState.ExpandFromDoor(westDoor, multiAreaGeo, DoorType.RoomDoor);
            Assert.NotNull(newGeo);

            Assert.True(westDoor.IsConnected);
            Assert.Equal("#9D00FF", westDoor.DoorBrush);
            Assert.True(newGeo.IsRoomGenerated(3)); // Area 3 automatically generated!
            Assert.False(newGeo.IsRoomGenerated(0));
            Assert.False(newGeo.IsRoomGenerated(1));
            Assert.False(newGeo.IsRoomGenerated(2));
        }
    }
}
