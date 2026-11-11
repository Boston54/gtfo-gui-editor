using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text.Json.Nodes;
using System.Windows.Input;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class EnemiesViewModel : ViewModelBase
{
    // Enemy datablocks are not edited in the UI, but imported custom records must
    // survive a subsequent export with all of their game-specific fields intact.
    internal Dictionary<uint, JsonObject> ImportedEnemyDataBlocks { get; } = new();

    public ObservableCollection<EnemyPopulationBlock> PopulationBlocks { get; } = new();
    public ObservableCollection<EnemyGroupBlock> Groups { get; } = new();
    public ObservableCollection<EnemyRoleData> PopulationSearchResults { get; } = new();
    public ObservableCollection<EnemyGroupBlock> GroupSearchResults { get; } = new();
    public ObservableCollection<EnemyEntry> Enemies { get; } = new(AlarmService.LoadEnemies());
    public ICommand ToggleGroupCommand { get; }
    public ICommand AddRoleDataCommand { get; }
    public ICommand RemoveRoleDataCommand { get; }
    public ICommand AddGroupRoleCommand { get; }
    public ICommand RemoveGroupRoleCommand { get; }
    public ICommand AddGroupCommand { get; }
    public ICommand RemoveGroupCommand { get; }

    public static readonly string[] Difficulties = { "Easy", "Medium", "Hard", "Biss", "Buss" };
    public static readonly string[] RoleNames =
    {
        "Melee", "Ranged", "Tank", "Lurker", "PureSneak", "Scout", "Patroller", "Hunter", "BirtherChild", "MiniBoss", "Boss"
    };
    public static readonly string[] Distributions =
    {
        "None", "Force_One", "Rel_05", "Rel_10", "Rel_15", "Rel_25", "Rel_50", "Rel_75", "Rel_100"
    };
    public static readonly string[] GroupTypes =
    {
        "Hibernate", "PureSneak", "Detect", "PureDetect", "Patrol", "Awake", "Hunter"
    };
    public string[] AvailableDifficulties => Difficulties;
    public string[] AvailableRoleNames => RoleNames;
    public string[] AvailableDistributions => Distributions;
    public string[] AvailableGroupTypes => GroupTypes;

    private string _populationSearchText = string.Empty;
    public string PopulationSearchText
    {
        get => _populationSearchText;
        set
        {
            if (_populationSearchText == value) return;
            _populationSearchText = value;
            OnPropertyChanged();
            RefreshPopulationSearch();
        }
    }

    private string _groupSearchText = string.Empty;
    public string GroupSearchText
    {
        get => _groupSearchText;
        set
        {
            if (_groupSearchText == value) return;
            _groupSearchText = value;
            OnPropertyChanged();
            RefreshGroupSearch();
        }
    }

    private EnemyPopulationBlock? _population;
    private bool _isRefreshingSpawningOptions;
    private bool _isRefreshingRoleOptions;
    private bool _isRefreshingPopulationSearch;
    private bool _hasImportedPopulation;
    private readonly HashSet<EnemyRoleData> _trackedPopulationRoles = new();
    public EnemyPopulationBlock Population
    {
        get => _population ??= PopulationBlocks.FirstOrDefault() ?? new EnemyPopulationBlock();
        set
        {
            if (_population == value) return;
            _population = value;
            _hasImportedPopulation = true;
            if (!PopulationBlocks.Contains(value)) PopulationBlocks.Clear();
            if (!PopulationBlocks.Contains(value)) PopulationBlocks.Add(value);
            AttachPopulationTracking(value);
            OnPropertyChanged();
        }
    }

    public EnemiesViewModel()
    {
        LoadVanillaDatablocks();
        if (PopulationBlocks.Count == 0)
        {
            var emptyPopulation = new EnemyPopulationBlock();
            emptyPopulation.RoleDatas.Add(new EnemyRoleData { Role = "Melee", Difficulty = "Easy" });
            PopulationBlocks.Add(emptyPopulation);
        }
        _population = PopulationBlocks[0];

        ToggleGroupCommand = new RelayCommand<EnemyGroupBlock>(g => { if (g != null) g.IsExpanded = !g.IsExpanded; });
        AddRoleDataCommand = new RelayCommand<object>(parameter =>
        {
            var population = parameter as EnemyPopulationBlock ?? Population;
            Population = population;
            population.RoleDatas.Add(new EnemyRoleData());
            RefreshSpawningOptions();
            RefreshPopulationSearch();
        });
        RemoveRoleDataCommand = new RelayCommand<object>(parameter =>
        {
            if (parameter is not EnemyRoleData role) return;

            foreach (var population in PopulationBlocks)
            {
                if (!population.RoleDatas.Remove(role)) continue;
                if (ReferenceEquals(Population, population))
                {
                    RefreshSpawningOptions();
                    RefreshPopulationSearch();
                }
                break;
            }
        });
        AddGroupRoleCommand = new RelayCommand<EnemyGroupBlock>(AddGroupRole);
        RemoveGroupRoleCommand = new RelayCommand<object>(parameter =>
        {
            if (parameter is not EnemyGroupRole role) return;
            foreach (var group in Groups.Where(g => g.Roles.Contains(role))) group.Roles.Remove(role);
        });
        AddGroupCommand = new RelayCommand(() => Groups.Add(new EnemyGroupBlock { Name = "New Enemy Group" }));
        RemoveGroupCommand = new RelayCommand<EnemyGroupBlock>(g => { if (g != null) Groups.Remove(g); });

        Groups.CollectionChanged += (_, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (EnemyGroupBlock group in e.NewItems)
                {
                    AttachGroupRoleTracking(group);
                    group.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(EnemyGroupBlock.IsExpanded)) return;
                        RefreshSpawningOptions();
                        RefreshGroupSearch();
                    };
                }
            }
            RefreshSpawningOptions();
            RefreshRoleOptions();
            RefreshGroupSearch();
        };
        foreach (var group in Groups)
            AttachGroupRoleTracking(group);
        AttachPopulationTracking(Population);
        RefreshSpawningOptions();
        RefreshPopulationSearch();
        RefreshGroupSearch();
    }

    private void LoadVanillaDatablocks()
    {
        string dir = AlarmService.GetDatablocksDirectory();
        string groupsPath = Path.Combine(dir, "EnemyGroupDataBlock.json");
        if (File.Exists(groupsPath))
        {
            try
            {
                if (JsonNode.Parse(File.ReadAllText(groupsPath)) is JsonObject obj &&
                    obj["Blocks"] is JsonArray blocks)
                {
                    foreach (var node in blocks.OfType<JsonObject>())
                    {
                        var group = new EnemyGroupBlock
                        {
                            Type = node["Type"]?.GetValue<string>() ?? "Hibernate",
                            Difficulty = node["Difficulty"]?.GetValue<string>() ?? "Easy",
                            SpawnPlacementType = node["SpawnPlacementType"]?.GetValue<string>() ?? "Default",
                            MaxScore = node["MaxScore"]?.GetValue<double>() ?? 0,
                            ScoreInAreaPaddingMulti = node["ScoreInAreaPaddingMulti"]?.GetValue<double>() ?? 3,
                            RelativeWeight = node["RelativeWeight"]?.GetValue<double>() ?? 1,
                            Name = node["name"]?.GetValue<string>() ?? string.Empty,
                            InternalEnabled = node["internalEnabled"]?.GetValue<bool>() ?? true,
                            PersistentId = node["persistentID"]?.GetValue<uint>() ?? 0
                        };
                        if (node["Roles"] is JsonArray roles)
                        {
                            foreach (var role in roles.OfType<JsonObject>())
                                group.Roles.Add(new EnemyGroupRole
                                {
                                    Role = role["Role"]?.GetValue<string>() ?? string.Empty,
                                    Distribution = role["Distribution"]?.GetValue<string>() ?? "Rel_100"
                                });
                        }
                        Groups.Add(group);
                    }
                }
            }
            catch { }
        }
    }

    public void RefreshSpawningOptions()
    {
        if (_isRefreshingSpawningOptions) return;
        _isRefreshingSpawningOptions = true;
        try
        {
            if (_hasImportedPopulation)
                EnemySpawningService.RefreshOptions(Groups, Population.RoleDatas, Enemies);
            RefreshRoleOptions();
        }
        finally
        {
            _isRefreshingSpawningOptions = false;
        }
    }

    private void AddGroupRole(EnemyGroupBlock? group)
    {
        if (group == null) return;
        group.Roles.Add(new EnemyGroupRole());
        RefreshRoleOptions();
    }

    private void AttachGroupRoleTracking(EnemyGroupBlock group)
    {
        foreach (var role in group.Roles)
            role.PropertyChanged += (_, _) => RefreshSpawningOptions();

        group.Roles.CollectionChanged += (_, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (EnemyGroupRole role in e.NewItems)
                    role.PropertyChanged += (_, _) => RefreshSpawningOptions();
            }
            RefreshSpawningOptions();
        };
    }

    private void RefreshPopulationSearch()
    {
        if (_isRefreshingPopulationSearch) return;
        _isRefreshingPopulationSearch = true;
        try
        {
            var query = PopulationSearchText?.Trim() ?? string.Empty;
            var matches = Population.RoleDatas
                .Where(role => string.IsNullOrEmpty(query) || MatchesPopulationRole(role, query))
                .ToList();
            if (PopulationSearchResults.SequenceEqual(matches)) return;

            PopulationSearchResults.Clear();
            foreach (var role in matches)
                PopulationSearchResults.Add(role);
        }
        finally
        {
            _isRefreshingPopulationSearch = false;
        }
    }

    private bool MatchesPopulationRole(EnemyRoleData role, string query)
    {
        var enemyName = role.SelectedEnemy?.DisplayText ?? Enemies.FirstOrDefault(e => e.PersistentId == role.Enemy)?.DisplayText;
        return Contains(role.Role, query)
            || Contains(role.Difficulty, query)
            || Contains(role.Enemy.ToString(), query)
            || Contains(enemyName, query)
            || Contains(role.Cost.ToString(), query)
            || Contains(role.Weight.ToString(), query);
    }

    private void RefreshGroupSearch()
    {
        GroupSearchResults.Clear();
        var query = GroupSearchText?.Trim() ?? string.Empty;
        foreach (var group in Groups)
        {
            if (string.IsNullOrEmpty(query) || MatchesGroup(group, query))
                GroupSearchResults.Add(group);
        }
    }

    private bool MatchesGroup(EnemyGroupBlock group, string query)
    {
        if (Contains(group.Name, query)
            || Contains(group.Difficulty, query)
            || Contains(group.SpawnPlacementType, query)
            || Contains(group.MaxScore.ToString(), query)
            || Contains(group.ScoreInAreaPaddingMulti.ToString(), query)
            || Contains(group.RelativeWeight.ToString(), query)
            || Contains(group.PersistentId.ToString(), query))
            return true;

        return group.Roles.Any(role =>
            Contains(role.Role, query) || Contains(role.Distribution, query));
    }

    private static bool Contains(string? value, string query) =>
        value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

    public void RefreshRoleOptions()
    {
        if (_isRefreshingRoleOptions) return;
        _isRefreshingRoleOptions = true;
        try
        {
            foreach (var group in Groups)
            {
                SetRoleOptions(group.AvailableRoles);
                foreach (var groupRole in group.Roles)
                {
                    SetRoleOptions(groupRole.AvailableRoles);
                    groupRole.IsMissingPopulationRole = !Population.RoleDatas.Any(populationRole =>
                        string.Equals(populationRole.Role, groupRole.Role, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(populationRole.Difficulty, group.Difficulty, StringComparison.OrdinalIgnoreCase));
                }
            }

            foreach (var populationRole in Population.RoleDatas)
            {
                if (!populationRole.EnemyEntries.SequenceEqual(Enemies))
                {
                    populationRole.EnemyEntries.Clear();
                    foreach (var enemy in Enemies) populationRole.EnemyEntries.Add(enemy);
                }

                var selectedEnemy = Enemies.FirstOrDefault(e => e.PersistentId == populationRole.Enemy);
                if (!ReferenceEquals(populationRole.SelectedEnemy, selectedEnemy))
                    populationRole.SelectedEnemy = selectedEnemy;
            }
            OnPropertyChanged(nameof(Groups));
        }
        finally
        {
            _isRefreshingRoleOptions = false;
        }
    }

    private void AttachPopulationTracking(EnemyPopulationBlock population)
    {
        population.RoleDatas.CollectionChanged += (_, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (EnemyRoleData role in e.NewItems)
                    AttachPopulationRoleTracking(role);
            }

            if (ReferenceEquals(Population, population))
            {
                RefreshSpawningOptions();
                RefreshPopulationSearch();
            }
        };

        foreach (var role in population.RoleDatas)
            AttachPopulationRoleTracking(role);
    }

    private void AttachPopulationRoleTracking(EnemyRoleData role)
    {
        if (!_trackedPopulationRoles.Add(role)) return;
        role.PropertyChanged += (_, _) =>
        {
            RefreshSpawningOptions();
            RefreshPopulationSearch();
        };
    }

    private static void SetRoleOptions(ObservableCollection<string> target)
    {
        if (target.SequenceEqual(RoleNames)) return;
        target.Clear();
        foreach (var role in RoleNames)
            target.Add(role);
    }
}
