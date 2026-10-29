using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class LightingViewModel : ViewModelBase
{
    private readonly List<LightSettingsBlock> _vanillaTemplates;
    private ObservableCollection<LightSettingsBlock> _blocks;
    private LightSettingsBlock? _selectedBlock;
    private string _statusText = string.Empty;
    private LightCategory _selectedCategory = LightCategory.General;
    private ObservableCollection<LightCategorySettingItem> _currentCategorySettings = new();
    private string _missingCategoriesWarning = string.Empty;

    // Create block prompt / modal state
    private bool _isCreatingBlock;
    private bool _isCreatingFresh = true;
    private bool _isCreatingFromExisting;
    private bool _isCreatingFromVanilla;
    private LightSettingsBlock? _selectedExistingBlock;
    private LightSettingsBlock? _selectedVanillaTemplate;
    private string _newBlockName = string.Empty;
    private uint _newBlockPersistentId;
    private string _creationErrorMessage = string.Empty;

    // Delete block confirmation modal state
    private bool _isConfirmingDelete;
    private LightSettingsBlock? _blockToDelete;

    public static LightCategory[] AvailableCategories => Enum.GetValues<LightCategory>();
    public LightCategory[] Categories => AvailableCategories;

    public ObservableCollection<LightSettingsBlock> Blocks
    {
        get => _blocks;
        private set => SetField(ref _blocks, value);
    }

    public ObservableCollection<LightSettingsBlock> AvailableTemplates { get; }

    public LightingViewModel()
        : this(new List<LightSettingsBlock>())
    {
    }

    public LightingViewModel(IEnumerable<LightSettingsBlock> initialBlocks)
    {
        var blockList = initialBlocks.ToList();
        _vanillaTemplates = LightSettingsService.LoadVanillaBlocks();
        _blocks = new ObservableCollection<LightSettingsBlock>(blockList);
        _blocks.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasExistingBlocks));
            OnPropertyChanged(nameof(HasExistingConfigs));
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        };

        AvailableTemplates = new ObservableCollection<LightSettingsBlock>(_vanillaTemplates);

        if (_blocks.Count > 0)
        {
            SelectedBlock = _blocks[0];
        }

        UpdateStatusText();
    }

    public LightSettingsBlock? SelectedBlock
    {
        get => _selectedBlock;
        set
        {
            if (_selectedBlock == value) return;

            if (_selectedBlock != null)
            {
                _selectedBlock.PropertyChanged -= SelectedBlock_PropertyChanged;
                _selectedBlock.LightCategorySettings.CollectionChanged -= LightCategorySettings_CollectionChanged;
            }

            if (SetField(ref _selectedBlock, value))
            {
                if (_selectedBlock != null)
                {
                    _selectedBlock.PropertyChanged += SelectedBlock_PropertyChanged;
                    _selectedBlock.LightCategorySettings.CollectionChanged += LightCategorySettings_CollectionChanged;
                }

                RefreshCategorySettings();
                CheckMissingCategoriesWarning();
                UpdateStatusText();
                OnPropertyChanged(nameof(HasSelectedBlock));
                OnPropertyChanged(nameof(HasDuplicatePersistentId));
                OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
            }
        }
    }

    private void SelectedBlock_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LightSettingsBlock.PersistentId))
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        }
    }

    private void LightCategorySettings_CollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RefreshCategorySettings();
        CheckMissingCategoriesWarning();
    }

    public LightCategory SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetField(ref _selectedCategory, value))
            {
                RefreshCategorySettings();
            }
        }
    }

    public ObservableCollection<LightCategorySettingItem> CurrentCategorySettings
    {
        get => _currentCategorySettings;
        private set
        {
            if (SetField(ref _currentCategorySettings, value))
            {
                OnPropertyChanged(nameof(HasCurrentCategorySettings));
            }
        }
    }

    public bool HasCurrentCategorySettings => _currentCategorySettings.Count > 0;

    public void RefreshCategorySettings()
    {
        if (_selectedBlock == null)
        {
            CurrentCategorySettings = new ObservableCollection<LightCategorySettingItem>();
            return;
        }

        var matching = _selectedBlock.LightCategorySettings
            .Where(item => item.Category == _selectedCategory)
            .ToList();

        CurrentCategorySettings = new ObservableCollection<LightCategorySettingItem>(matching);
    }

    public bool HasMissingCategories => !string.IsNullOrEmpty(_missingCategoriesWarning);

    public string MissingCategoriesWarning
    {
        get => _missingCategoriesWarning;
        private set
        {
            if (SetField(ref _missingCategoriesWarning, value))
            {
                OnPropertyChanged(nameof(HasMissingCategories));
            }
        }
    }

    public void CheckMissingCategoriesWarning()
    {
        if (_selectedBlock == null)
        {
            MissingCategoriesWarning = string.Empty;
            return;
        }

        var definedCategories =
            new HashSet<LightCategory>(_selectedBlock.LightCategorySettings.Select(s => s.Category));
        var missing = AvailableCategories.Where(c => !definedCategories.Contains(c)).ToList();

        if (missing.Count > 0)
        {
            string missingNames = string.Join(", ", missing);
            MissingCategoriesWarning =
                $"Warning: The following lighting categories have no defined options in this config: {missingNames}";
        }
        else
        {
            MissingCategoriesWarning = string.Empty;
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
            return
                $"Warning: Persistent ID {_selectedBlock.PersistentId} is used by {count} blocks in this datablock! IDs must be unique.";
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    private void UpdateStatusText()
    {
        if (_selectedBlock != null)
        {
            StatusText =
                $"{_blocks.Count} configs loaded. Editing: [{_selectedBlock.PersistentId}] {_selectedBlock.Name}";
        }
        else
        {
            StatusText = $"{_blocks.Count} configs loaded.";
        }
    }

    // Options Management
    public void AddOption()
    {
        if (_selectedBlock == null) return;

        var newItem = new LightCategorySettingItem
        {
            Category = _selectedCategory,
            On = true,
            Chance = 1.0,
            ChanceBroken = 0.0,
            IntensityMul = 1.0,
            Weight = 1.0,
            Color = new LightColor { R = 1.0, G = 1.0, B = 1.0, A = 0.0 }
        };

        _selectedBlock.LightCategorySettings.Add(newItem);
        if (!_currentCategorySettings.Contains(newItem))
        {
            _currentCategorySettings.Add(newItem);
            OnPropertyChanged(nameof(HasCurrentCategorySettings));
        }

        CheckMissingCategoriesWarning();
    }

    public void RemoveOption(LightCategorySettingItem? item)
    {
        if (_selectedBlock == null || item == null) return;
        _selectedBlock.LightCategorySettings.Remove(item);
        _currentCategorySettings.Remove(item);
        OnPropertyChanged(nameof(HasCurrentCategorySettings));
        CheckMissingCategoriesWarning();
    }

    // Dialog / Prompt properties
    public bool HasExistingBlocks => _blocks.Count > 0;

    public bool HasExistingConfigs => _blocks.Count > 0;

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
                _isCreatingFromExisting = false;
                _isCreatingFromVanilla = false;
                OnPropertyChanged(nameof(IsCreatingFromExisting));
                OnPropertyChanged(nameof(IsCreatingFromVanilla));
                OnPropertyChanged(nameof(IsCreatingFromTemplate));
            }
        }
    }

    public bool IsCreatingFromExisting
    {
        get => _isCreatingFromExisting;
        set
        {
            if (SetField(ref _isCreatingFromExisting, value) && value)
            {
                _isCreatingFresh = false;
                _isCreatingFromVanilla = false;
                OnPropertyChanged(nameof(IsCreatingFresh));
                OnPropertyChanged(nameof(IsCreatingFromVanilla));
                OnPropertyChanged(nameof(IsCreatingFromTemplate));
                if (SelectedExistingBlock != null)
                {
                    if (!string.IsNullOrWhiteSpace(SelectedExistingBlock.Name))
                    {
                        NewBlockName = $"{SelectedExistingBlock.Name}_Copy";
                    }
                }
            }
        }
    }

    public bool IsCreatingFromVanilla
    {
        get => _isCreatingFromVanilla;
        set
        {
            if (SetField(ref _isCreatingFromVanilla, value) && value)
            {
                _isCreatingFresh = false;
                _isCreatingFromExisting = false;
                OnPropertyChanged(nameof(IsCreatingFresh));
                OnPropertyChanged(nameof(IsCreatingFromExisting));
                OnPropertyChanged(nameof(IsCreatingFromTemplate));
                if (SelectedVanillaTemplate != null)
                {
                    if (!string.IsNullOrWhiteSpace(SelectedVanillaTemplate.Name))
                    {
                        NewBlockName = SelectedVanillaTemplate.Name;
                    }
                }
            }
        }
    }

    public bool IsCreatingFromTemplate
    {
        get => _isCreatingFromVanilla || _isCreatingFromExisting;
        set
        {
            if (value)
            {
                IsCreatingFromVanilla = true;
            }
            else
            {
                IsCreatingFresh = true;
            }
        }
    }

    public LightSettingsBlock? SelectedExistingBlock
    {
        get => _selectedExistingBlock;
        set
        {
            if (SetField(ref _selectedExistingBlock, value) && value != null)
            {
                if (IsCreatingFromExisting)
                {
                    if (!string.IsNullOrWhiteSpace(value.Name))
                    {
                        NewBlockName = $"{value.Name}_Copy";
                    }
                }
            }
        }
    }

    public LightSettingsBlock? SelectedVanillaTemplate
    {
        get => _selectedVanillaTemplate;
        set
        {
            if (SetField(ref _selectedVanillaTemplate, value) && value != null)
            {
                if (IsCreatingFromVanilla || IsCreatingFromTemplate)
                {
                    if (!string.IsNullOrWhiteSpace(value.Name))
                    {
                        NewBlockName = value.Name;
                    }
                }
            }
        }
    }

    public LightSettingsBlock? SelectedTemplate
    {
        get => _isCreatingFromExisting ? _selectedExistingBlock : _selectedVanillaTemplate;
        set
        {
            if (_isCreatingFromExisting)
            {
                SelectedExistingBlock = value;
            }
            else
            {
                SelectedVanillaTemplate = value;
            }
        }
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
        NewBlockName = $"Custom_Lighting_{NewBlockPersistentId}";
        IsCreatingFresh = true;
        _isCreatingFromExisting = false;
        _isCreatingFromVanilla = false;
        OnPropertyChanged(nameof(IsCreatingFresh));
        OnPropertyChanged(nameof(IsCreatingFromExisting));
        OnPropertyChanged(nameof(IsCreatingFromVanilla));
        OnPropertyChanged(nameof(IsCreatingFromTemplate));

        SelectedExistingBlock = _selectedBlock ?? _blocks.FirstOrDefault();
        SelectedVanillaTemplate = AvailableTemplates.FirstOrDefault();
        CreationErrorMessage = string.Empty;
        IsCreatingBlock = true;
    }

    public void OpenDuplicateBlockDialog(LightSettingsBlock? block = null)
    {
        var target = block ?? _selectedBlock ?? _blocks.FirstOrDefault();
        OpenCreateBlockDialog();
        if (target != null)
        {
            SelectedExistingBlock = target;
            IsCreatingFromExisting = true;
        }
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

        LightSettingsBlock newBlock;
        if (IsCreatingFromExisting)
        {
            if (SelectedExistingBlock == null)
            {
                CreationErrorMessage = "Please select an existing config to duplicate.";
                return false;
            }

            newBlock = LightSettingsService.CreateBlockFromTemplate(SelectedExistingBlock, NewBlockPersistentId,
                NewBlockName.Trim());
        }
        else if (IsCreatingFromVanilla)
        {
            if (SelectedVanillaTemplate == null)
            {
                CreationErrorMessage = "Please select a vanilla config to import.";
                return false;
            }

            newBlock = LightSettingsService.CreateBlockFromTemplate(SelectedVanillaTemplate, NewBlockPersistentId,
                NewBlockName.Trim());
        }
        else
        {
            newBlock = LightSettingsService.CreateFreshBlock(NewBlockPersistentId, NewBlockName.Trim());
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

    public LightSettingsBlock? BlockToDelete
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
            if (target == null) return "Are you sure you want to delete this config?";
            return $"Are you sure you want to delete config [{target.PersistentId}] \"{target.Name}\"?";
        }
    }

    public void RequestDeleteSelectedBlock()
    {
        if (_selectedBlock == null) return;
        RequestDeleteBlock(_selectedBlock);
    }

    public void RequestDeleteBlock(LightSettingsBlock? block)
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

    public bool DeleteBlock(LightSettingsBlock block)
    {
        int index = _blocks.IndexOf(block);
        if (index < 0) return false;

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
        StatusText = $"Deleted config [{block.PersistentId}] {block.Name}";
        return true;
    }
}