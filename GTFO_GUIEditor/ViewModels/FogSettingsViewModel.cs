using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class FogSettingsViewModel : ViewModelBase
{
    private readonly List<FogSettingsBlock> _vanillaTemplates;
    private ObservableCollection<FogSettingsBlock> _blocks;
    private FogSettingsBlock? _selectedBlock;
    private string _statusText = string.Empty;

    // Create block prompt / modal state
    private bool _isCreatingBlock;
    private bool _isCreatingFresh = true;
    private bool _isCreatingFromTemplate;
    private FogSettingsBlock? _selectedTemplate;
    private string _newBlockName = string.Empty;
    private uint _newBlockPersistentId;
    private string _creationErrorMessage = string.Empty;

    // Delete block confirmation modal state
    private bool _isConfirmingDelete;
    private FogSettingsBlock? _blockToDelete;

    public ObservableCollection<FogSettingsBlock> Blocks
    {
        get => _blocks;
        private set => SetField(ref _blocks, value);
    }

    public ObservableCollection<FogSettingsBlock> AvailableTemplates { get; }

    public FogSettingsViewModel()
        : this(FogSettingsService.LoadVanillaBlocks())
    {
    }

    public FogSettingsViewModel(IEnumerable<FogSettingsBlock> initialBlocks)
    {
        var blockList = initialBlocks.ToList();
        _vanillaTemplates = blockList.Select(b => b.Clone()).ToList();
        _blocks = new ObservableCollection<FogSettingsBlock>(blockList);
        _blocks.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        };

        AvailableTemplates = new ObservableCollection<FogSettingsBlock>(_vanillaTemplates);

        if (_blocks.Count > 0)
        {
            SelectedBlock = _blocks[0];
        }

        UpdateStatusText();
    }

    public FogSettingsBlock? SelectedBlock
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
        if (e.PropertyName == nameof(FogSettingsBlock.PersistentId))
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        }
    }

    public bool HasSelectedBlock => SelectedBlock != null;

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
                return $"Warning: Persistent ID {SelectedBlock.PersistentId} is shared by {count} fog configs. Each config should have a unique ID.";
            }
            return string.Empty;
        }
    }

    private void UpdateStatusText()
    {
        if (SelectedBlock != null)
        {
            StatusText = $"Selected: [{SelectedBlock.PersistentId}] {SelectedBlock.Name} ({_blocks.Count} configs loaded)";
        }
        else
        {
            StatusText = $"No fog config selected ({_blocks.Count} configs loaded)";
        }
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

    public FogSettingsBlock? SelectedTemplate
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
        NewBlockName = $"Custom_Fog_{NewBlockPersistentId}";
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
            CreationErrorMessage = "Config name cannot be empty.";
            return false;
        }

        if (NewBlockPersistentId == 0)
        {
            CreationErrorMessage = "Persistent ID must be greater than 0.";
            return false;
        }

        if (_blocks.Any(b => b.PersistentId == NewBlockPersistentId))
        {
            CreationErrorMessage = $"A config with Persistent ID {NewBlockPersistentId} already exists.";
            return false;
        }

        FogSettingsBlock newBlock;
        if (IsCreatingFromTemplate && SelectedTemplate != null)
        {
            newBlock = FogSettingsService.CreateBlockFromTemplate(SelectedTemplate, NewBlockPersistentId, NewBlockName.Trim());
        }
        else
        {
            newBlock = FogSettingsService.CreateFreshBlock(NewBlockPersistentId, NewBlockName.Trim());
        }

        _blocks.Add(newBlock);
        SelectedBlock = newBlock;
        IsCreatingBlock = false;
        CreationErrorMessage = string.Empty;
        UpdateStatusText();
        StatusText = $"Created config [{newBlock.PersistentId}] {newBlock.Name}";
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

    public FogSettingsBlock? BlockToDelete
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
        ? $"Are you sure you want to delete fog config '{BlockToDelete.Name}' ({BlockToDelete.PersistentId})?\n\nThis action cannot be undone."
        : string.Empty;

    public void RequestDeleteSelectedBlock()
    {
        if (SelectedBlock == null) return;
        BlockToDelete = SelectedBlock;
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
        StatusText = $"Deleted config [{blockToRemove.PersistentId}] {blockToRemove.Name}";
    }

    public void CancelDeleteBlock()
    {
        IsConfirmingDelete = false;
        BlockToDelete = null;
    }
}
