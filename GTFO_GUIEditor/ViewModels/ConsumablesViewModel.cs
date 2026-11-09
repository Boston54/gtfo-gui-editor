using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class ConsumablesViewModel : ViewModelBase
{
    private readonly List<ConsumableDistributionBlock> _vanillaTemplates;
    private ObservableCollection<ConsumableDistributionBlock> _blocks;
    private ConsumableDistributionBlock? _selectedBlock;
    private string _statusText = string.Empty;
    private string _searchText = string.Empty;
    private bool _isExpanded;

    // Create block prompt / modal state
    private bool _isCreatingBlock;
    private bool _isCreatingFresh = true;
    private bool _isCreatingFromTemplate;
    private ConsumableDistributionBlock? _selectedTemplate;
    private string _newBlockName = string.Empty;
    private uint _newBlockPersistentId;
    private string _creationErrorMessage = string.Empty;

    // Delete block confirmation modal state
    private bool _isConfirmingDelete;
    private ConsumableDistributionBlock? _blockToDelete;

    public static Consumables[] AvailableConsumables => Enum.GetValues<Consumables>();

    public ICommand AddSpawnDataItemCommand { get; }
    public ICommand RemoveSpawnDataItemCommand { get; }
    public ICommand ToggleBlockCommand { get; }
    public ICommand DuplicateBlockCommand { get; }
    public ICommand DeleteBlockCommand { get; }
    public ObservableCollection<ConsumableDistributionBlock> FilteredBlocks { get; } = new();

    public ObservableCollection<ConsumableDistributionBlock> Blocks
    {
        get => _blocks;
        private set => SetField(ref _blocks, value);
    }

    public ObservableCollection<ConsumableDistributionBlock> AvailableTemplates { get; }

    public ConsumablesViewModel()
        : this(new List<ConsumableDistributionBlock>())
    {
    }

    public ConsumablesViewModel(IEnumerable<ConsumableDistributionBlock> initialBlocks)
    {
        var blockList = initialBlocks.ToList();
        _vanillaTemplates = ConsumableDistributionService.LoadVanillaBlocks();
        _blocks = new ObservableCollection<ConsumableDistributionBlock>(blockList);
        _blocks.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
            RefreshFilteredBlocks();
        };

        AvailableTemplates = new ObservableCollection<ConsumableDistributionBlock>(_vanillaTemplates);

        AddSpawnDataItemCommand = new RelayCommand(AddSpawnDataItem);
        RemoveSpawnDataItemCommand = new RelayCommand<ConsumableDistributionItem>(RemoveSpawnDataItem);
        ToggleBlockCommand = new RelayCommand<ConsumableDistributionBlock>(ToggleBlockExpanded);
        DuplicateBlockCommand = new RelayCommand<ConsumableDistributionBlock>(DuplicateBlock);
        DeleteBlockCommand = new RelayCommand<ConsumableDistributionBlock>(RequestDeleteBlock);
        if (_blocks.Count > 0)
        {
            SelectedBlock = _blocks[0];
        }
        RefreshFilteredBlocks();

        UpdateStatusText();
    }

    public ConsumableDistributionBlock? SelectedBlock
    {
        get => _selectedBlock;
        set
        {
            if (_selectedBlock == value) return;

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

                UpdateStatusText();
                OnPropertyChanged(nameof(HasSelectedBlock));
                OnPropertyChanged(nameof(HasDuplicatePersistentId));
                OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
            }
        }
    }

    private void SelectedBlock_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConsumableDistributionBlock.PersistentId))
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        }
    }

    public bool HasSelectedBlock => SelectedBlock != null;

    public bool IsExpanded
    {
        get => _isExpanded;
        private set => SetField(ref _isExpanded, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                RefreshFilteredBlocks();
            }
        }
    }

    private void RefreshFilteredBlocks()
    {
        FilteredBlocks.Clear();
        foreach (var block in _blocks.Where(block =>
                     string.IsNullOrWhiteSpace(_searchText) ||
                     block.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                     block.DisplayText.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
        {
            FilteredBlocks.Add(block);
        }
    }

    public void ToggleBlockExpanded(ConsumableDistributionBlock? block)
    {
        if (block == null) return;
        block.IsExpanded = !block.IsExpanded;
        SelectedBlock = block;
    }

    public void DuplicateBlock(ConsumableDistributionBlock? source)
    {
        if (source == null) return;
        uint newId = _blocks.Count == 0 ? 1 : _blocks.Max(block => block.PersistentId) + 1;
        var copy = ConsumableDistributionService.CreateBlockFromTemplate(
            source,
            newId,
            string.IsNullOrWhiteSpace(source.Name) ? "Consumable Distribution_copy" : $"{source.Name}_copy");
        _blocks.Add(copy);
        SelectedBlock = copy;
        copy.IsExpanded = true;
        StatusText = $"Duplicated distribution {source.Name}";
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public bool HasDuplicatePersistentId
    {
        get
        {
            if (SelectedBlock == null) return false;
            return _blocks.Count(b => b.PersistentId == SelectedBlock.PersistentId) > 1;
        }
    }

    public string DuplicatePersistentIdWarning
    {
        get
        {
            if (SelectedBlock == null) return string.Empty;
            int count = _blocks.Count(b => b.PersistentId == SelectedBlock.PersistentId);
            if (count > 1)
            {
                return $"Warning: This identifier is shared by {count} consumable distributions.";
            }
            return string.Empty;
        }
    }

    private void UpdateStatusText()
    {
        if (SelectedBlock != null)
        {
            StatusText = $"Selected: {SelectedBlock.Name} ({_blocks.Count} distributions loaded)";
        }
        else
        {
            StatusText = $"No consumable distribution selected ({_blocks.Count} distributions loaded)";
        }
    }

    public void AddSpawnDataItem()
    {
        if (SelectedBlock == null) return;
        SelectedBlock.SpawnData.Add(new ConsumableDistributionItem(Consumables.Glowstick, 1.0));
    }

    public void RemoveSpawnDataItem(ConsumableDistributionItem? item)
    {
        if (SelectedBlock == null || item == null) return;
        SelectedBlock.SpawnData.Remove(item);
    }

    // Modal Create/Delete properties and methods

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

    public ConsumableDistributionBlock? SelectedTemplate
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
        NewBlockName = "Custom_ConsumableDistribution";
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
            CreationErrorMessage = "Distribution name cannot be empty.";
            return false;
        }

        if (NewBlockPersistentId == 0)
        {
            CreationErrorMessage = "Unable to assign an internal identifier to this distribution.";
            return false;
        }

        if (_blocks.Any(b => b.PersistentId == NewBlockPersistentId))
        {
            CreationErrorMessage = "A distribution with this internal identifier already exists.";
            return false;
        }

        ConsumableDistributionBlock newBlock;
        if (IsCreatingFromTemplate && SelectedTemplate != null)
        {
            newBlock = ConsumableDistributionService.CreateBlockFromTemplate(SelectedTemplate, NewBlockPersistentId, NewBlockName.Trim());
        }
        else
        {
            newBlock = ConsumableDistributionService.CreateFreshBlock(NewBlockPersistentId, NewBlockName.Trim());
        }

        _blocks.Add(newBlock);
        SelectedBlock = newBlock;
        IsCreatingBlock = false;
        CreationErrorMessage = string.Empty;
        UpdateStatusText();
        StatusText = $"Created distribution {newBlock.Name}";
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

    public ConsumableDistributionBlock? BlockToDelete
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

    public string DeleteConfirmationMessage => BlockToDelete != null
        ? $"Are you sure you want to delete consumable distribution '{BlockToDelete.Name}'?\n\nThis action cannot be undone."
        : string.Empty;

    public void RequestDeleteSelectedBlock()
    {
        if (SelectedBlock == null) return;
        BlockToDelete = SelectedBlock;
        IsConfirmingDelete = true;
    }

    public void RequestDeleteBlock(ConsumableDistributionBlock? block)
    {
        if (block == null) return;
        BlockToDelete = block;
        IsConfirmingDelete = true;
    }

    public void ConfirmDeleteBlock()
    {
        if (BlockToDelete == null) return;

        var blockToRemove = BlockToDelete;
        int index = _blocks.IndexOf(blockToRemove);
        _blocks.Remove(blockToRemove);

        if (SelectedBlock == blockToRemove)
        {
            if (_blocks.Count > 0)
            {
                int newIndex = Math.Clamp(index, 0, _blocks.Count - 1);
                SelectedBlock = _blocks[newIndex];
            }
            else
            {
                SelectedBlock = null;
            }
        }

        IsConfirmingDelete = false;
        BlockToDelete = null;
        UpdateStatusText();
        StatusText = $"Deleted distribution {blockToRemove.Name}";
    }

    public void CancelDeleteBlock()
    {
        IsConfirmingDelete = false;
        BlockToDelete = null;
    }
}
