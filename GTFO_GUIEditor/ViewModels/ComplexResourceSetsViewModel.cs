using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class ComplexResourceSetsViewModel : ViewModelBase
{
    private readonly List<ComplexResourceSetBlock> _vanillaTemplates;
    private ObservableCollection<ComplexResourceSetBlock> _blocks;
    private ComplexResourceSetBlock? _selectedBlock;
    private string _statusText = string.Empty;

    // Category / Prefabs management
    private string _selectedCategory = nameof(ComplexResourceSetBlock.GeomorphTiles_1x1);
    private ObservableCollection<PrefabItem> _currentCategoryItems = new();
    private PrefabItem? _selectedPrefabItem;

    // Create block prompt / dialog state
    private bool _isCreatingBlock;
    private bool _isCreatingFresh = true;
    private bool _isCreatingFromTemplate;
    private ComplexResourceSetBlock? _selectedTemplate;
    private string _newBlockName = string.Empty;
    private uint _newBlockPersistentId;
    private string _creationErrorMessage = string.Empty;

    // Delete block confirmation dialog state
    private bool _isConfirmingDelete;
    private ComplexResourceSetBlock? _blockToDelete;

    public static ComplexType[] AvailableComplexTypes => Enum.GetValues<ComplexType>();
    public static SubComplexType[] AvailableSubComplexTypes => Enum.GetValues<SubComplexType>();
    public static BundleNameType[] AvailableBundleNames => Enum.GetValues<BundleNameType>();
    public static TransitionDirection[] AvailableTransitionDirections => Enum.GetValues<TransitionDirection>();
    public static LevelProgression[] AvailableLevelProgressions => Enum.GetValues<LevelProgression>();
    public static ShardType[] AvailableShards => Enum.GetValues<ShardType>();

    public ComplexType[] ComplexTypes => AvailableComplexTypes;
    public SubComplexType[] SubComplexTypes => AvailableSubComplexTypes;
    public BundleNameType[] BundleNames => AvailableBundleNames;
    public TransitionDirection[] TransitionDirections => AvailableTransitionDirections;
    public LevelProgression[] LevelProgressions => AvailableLevelProgressions;
    public ShardType[] Shards => AvailableShards;

    public ObservableCollection<string> Categories { get; } = new(ComplexResourceSetBlock.CategoryNames);

    public ComplexResourceSetsViewModel()
        : this(ComplexResourceSetService.LoadVanillaBlocks())
    {
    }

    public ComplexResourceSetsViewModel(IEnumerable<ComplexResourceSetBlock> initialBlocks)
    {
        var blockList = initialBlocks.ToList();
        _vanillaTemplates = blockList.Select(b => b.Clone()).ToList();
        _blocks = new ObservableCollection<ComplexResourceSetBlock>(blockList);
        _blocks.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        };
        AvailableTemplates = new ObservableCollection<ComplexResourceSetBlock>(_vanillaTemplates);

        if (_blocks.Count > 0)
        {
            SelectedBlock = _blocks[0];
        }

        UpdateStatusText();
    }

    public ObservableCollection<ComplexResourceSetBlock> Blocks
    {
        get => _blocks;
        private set => SetField(ref _blocks, value);
    }

    public ObservableCollection<ComplexResourceSetBlock> AvailableTemplates { get; }

    public ComplexResourceSetBlock? SelectedBlock
    {
        get => _selectedBlock;
        set
        {
            if (_selectedBlock != null)
            {
                _selectedBlock.PropertyChanged -= SelectedBlock_PropertyChanged;
            }

            if (SetField(ref _selectedBlock, value))
            {
                if (_selectedBlock != null)
                {
                    _selectedBlock.PropertyChanged += SelectedBlock_PropertyChanged;
                }

                RefreshCategoryItems();
                UpdateStatusText();
                OnPropertyChanged(nameof(HasSelectedBlock));
                OnPropertyChanged(nameof(HasDuplicatePersistentId));
                OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
            }
        }
    }

    private void SelectedBlock_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ComplexResourceSetBlock.PersistentId))
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        }
    }

    public bool HasSelectedBlock => _selectedBlock != null;

    public bool HasDuplicatePersistentId
    {
        get
        {
            if (_selectedBlock == null) return false;
            return _blocks.Count(b => b.PersistentId == _selectedBlock.PersistentId) > 1;
        }
    }

    public string DuplicatePersistentIdWarning
    {
        get
        {
            if (_selectedBlock == null || !HasDuplicatePersistentId) return string.Empty;
            int count = _blocks.Count(b => b.PersistentId == _selectedBlock.PersistentId);
            return $"Warning: Persistent ID {_selectedBlock.PersistentId} is used by {count} blocks in this datablock! IDs must be unique.";
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetField(ref _selectedCategory, value))
            {
                RefreshCategoryItems();
            }
        }
    }

    public ObservableCollection<PrefabItem> CurrentCategoryItems
    {
        get => _currentCategoryItems;
        private set => SetField(ref _currentCategoryItems, value);
    }

    public PrefabItem? SelectedPrefabItem
    {
        get => _selectedPrefabItem;
        set => SetField(ref _selectedPrefabItem, value);
    }

    // Dialog / Prompt properties
    public bool IsCreatingBlock
    {
        get => _isCreatingBlock;
        set => SetField(ref _isCreatingBlock, value);
    }

    public bool IsCreatingFresh
    {
        get => _isCreatingFresh;
        set
        {
            if (SetField(ref _isCreatingFresh, value) && value)
            {
                _isCreatingFromTemplate = false;
                OnPropertyChanged(nameof(IsCreatingFromTemplate));
            }
        }
    }

    public bool IsCreatingFromTemplate
    {
        get => _isCreatingFromTemplate;
        set
        {
            if (SetField(ref _isCreatingFromTemplate, value) && value)
            {
                _isCreatingFresh = false;
                OnPropertyChanged(nameof(IsCreatingFresh));
            }
        }
    }

    public ComplexResourceSetBlock? SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetField(ref _selectedTemplate, value);
    }

    public string NewBlockName
    {
        get => _newBlockName;
        set => SetField(ref _newBlockName, value);
    }

    public uint NewBlockPersistentId
    {
        get => _newBlockPersistentId;
        set => SetField(ref _newBlockPersistentId, value);
    }

    public string CreationErrorMessage
    {
        get => _creationErrorMessage;
        set
        {
            if (SetField(ref _creationErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasCreationErrorMessage));
            }
        }
    }

    public bool HasCreationErrorMessage => !string.IsNullOrWhiteSpace(_creationErrorMessage);

    public void RefreshCategoryItems()
    {
        if (_selectedBlock == null || string.IsNullOrEmpty(_selectedCategory))
        {
            CurrentCategoryItems = new ObservableCollection<PrefabItem>();
            return;
        }

        var list = _selectedBlock.GetListByCategory(_selectedCategory);
        CurrentCategoryItems = new ObservableCollection<PrefabItem>(list);
    }

    public void AddPrefabItem()
    {
        if (_selectedBlock == null) return;

        var newItem = new PrefabItem
        {
            Prefab = "Assets/AssetPrefabs/...",
            SubComplex = _selectedBlock.PrimareSubComplexUsed,
            Shard = ShardType.S1
        };

        var list = _selectedBlock.GetListByCategory(_selectedCategory);
        list.Add(newItem);
        CurrentCategoryItems.Add(newItem);
        SelectedPrefabItem = newItem;
    }

    public void RemoveSelectedPrefabItem()
    {
        if (_selectedBlock == null || _selectedPrefabItem == null) return;

        var list = _selectedBlock.GetListByCategory(_selectedCategory);
        list.Remove(_selectedPrefabItem);
        CurrentCategoryItems.Remove(_selectedPrefabItem);
        SelectedPrefabItem = null;
    }

    public uint GenerateRandomPersistentId()
    {
        var existingIds = new HashSet<uint>(_blocks.Select(b => b.PersistentId));
        uint id;
        do
        {
            id = (uint)Random.Shared.Next(1000, 100000000);
        } while (existingIds.Contains(id));
        return id;
    }

    public void OpenCreateBlockDialog()
    {
        NewBlockPersistentId = GenerateRandomPersistentId();
        NewBlockName = $"Custom_Complex_{NewBlockPersistentId}";
        IsCreatingFresh = true;
        IsCreatingFromTemplate = false;
        SelectedTemplate = AvailableTemplates.FirstOrDefault();
        CreationErrorMessage = string.Empty;
        IsCreatingBlock = true;
    }

    public bool ConfirmCreateBlock()
    {
        if (string.IsNullOrWhiteSpace(NewBlockName))
        {
            CreationErrorMessage = "Block name cannot be empty.";
            return false;
        }

        if (NewBlockPersistentId == 0)
        {
            CreationErrorMessage = "Persistent ID must be greater than 0.";
            return false;
        }

        if (_blocks.Any(b => b.PersistentId == NewBlockPersistentId))
        {
            CreationErrorMessage = $"A block with Persistent ID {NewBlockPersistentId} already exists.";
            return false;
        }

        ComplexResourceSetBlock newBlock;
        if (IsCreatingFromTemplate && SelectedTemplate != null)
        {
            newBlock = ComplexResourceSetService.CreateBlockFromTemplate(SelectedTemplate, NewBlockPersistentId, NewBlockName.Trim());
        }
        else
        {
            newBlock = ComplexResourceSetService.CreateFreshBlock(NewBlockPersistentId, NewBlockName.Trim());
        }

        _blocks.Add(newBlock);
        SelectedBlock = newBlock;
        IsCreatingBlock = false;
        CreationErrorMessage = string.Empty;
        UpdateStatusText();
        StatusText = $"Created block [{newBlock.PersistentId}] {newBlock.Name}";
        return true;
    }

    public void CancelCreateBlock()
    {
        IsCreatingBlock = false;
        CreationErrorMessage = string.Empty;
    }

    public bool IsConfirmingDelete
    {
        get => _isConfirmingDelete;
        set => SetField(ref _isConfirmingDelete, value);
    }

    public ComplexResourceSetBlock? BlockToDelete
    {
        get => _blockToDelete;
        set
        {
            if (SetField(ref _blockToDelete, value))
            {
                OnPropertyChanged(nameof(DeleteConfirmationMessage));
            }
        }
    }

    public string DeleteConfirmationMessage
    {
        get
        {
            var target = _blockToDelete ?? _selectedBlock;
            if (target == null) return "Are you sure you want to delete this block?";
            return $"Are you sure you want to delete block [{target.PersistentId}] \"{target.Name}\"?";
        }
    }

    public void RequestDeleteSelectedBlock()
    {
        if (_selectedBlock == null) return;
        RequestDeleteBlock(_selectedBlock);
    }

    public void RequestDeleteBlock(ComplexResourceSetBlock? block)
    {
        if (block == null) return;
        BlockToDelete = block;
        IsConfirmingDelete = true;
    }

    public bool ConfirmDeleteBlock()
    {
        var target = BlockToDelete ?? _selectedBlock;
        IsConfirmingDelete = false;
        BlockToDelete = null;

        if (target == null) return false;
        return DeleteBlock(target);
    }

    public void CancelDeleteBlock()
    {
        IsConfirmingDelete = false;
        BlockToDelete = null;
    }

    public bool DeleteSelectedBlock()
    {
        if (_selectedBlock == null) return false;
        return DeleteBlock(_selectedBlock);
    }

    public bool DeleteBlock(ComplexResourceSetBlock? block)
    {
        if (block == null) return false;
        int index = _blocks.IndexOf(block);
        if (index < 0) return false;

        string deletedName = block.Name;
        uint deletedId = block.PersistentId;

        _blocks.RemoveAt(index);

        if (_selectedBlock == block)
        {
            if (_blocks.Count > 0)
            {
                int newIndex = Math.Min(index, _blocks.Count - 1);
                SelectedBlock = _blocks[newIndex];
            }
            else
            {
                SelectedBlock = null;
            }
        }

        UpdateStatusText();
        StatusText = $"Deleted block [{deletedId}] {deletedName}. {_blocks.Count:N0} blocks remaining.";
        return true;
    }

    private void UpdateStatusText()
    {
        StatusText = $"{_blocks.Count:N0} blocks loaded";
    }
}
