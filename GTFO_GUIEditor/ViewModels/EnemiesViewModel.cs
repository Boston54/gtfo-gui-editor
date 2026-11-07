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
    public static readonly string[] RoleNames = { "Melee", "Ranged", "Big", "Special", "Scout", "Boss" };
    public static readonly string[] Distributions =
    {
        "Rel_0","Rel_5","Rel_10","Rel_15","Rel_20","Rel_25","Rel_30","Rel_35","Rel_40",
        "Rel_45","Rel_50","Rel_55","Rel_60","Rel_65","Rel_70","Rel_75","Rel_80","Rel_85",
        "Rel_90","Rel_95","Rel_100","Force_One"
    };
    public string[] AvailableDifficulties => Difficulties;
    public string[] AvailableRoleNames => RoleNames;
    public string[] AvailableDistributions => Distributions;

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
    public EnemyPopulationBlock Population
    {
        get => _population ??= PopulationBlocks.FirstOrDefault() ?? new EnemyPopulationBlock();
        set
        {
            if (_population == value) return;
            _population = value;
            if (!PopulationBlocks.Contains(value)) PopulationBlocks.Clear();
            if (!PopulationBlocks.Contains(value)) PopulationBlocks.Add(value);
            OnPropertyChanged();
        }
    }

    public EnemiesViewModel()
    {
        LoadVanillaDatablocks();
        if (PopulationBlocks.Count == 0) PopulationBlocks.Add(new EnemyPopulationBlock());
        _population = PopulationBlocks[0];

        ToggleGroupCommand = new RelayCommand<EnemyGroupBlock>(g => { if (g != null) g.IsExpanded = !g.IsExpanded; });
        AddRoleDataCommand = new RelayCommand(() => Population.RoleDatas.Add(new EnemyRoleData()));
        RemoveRoleDataCommand = new RelayCommand<EnemyRoleData>(r => { if (r != null) Population.RoleDatas.Remove(r); });
        AddGroupRoleCommand = new RelayCommand<EnemyGroupBlock>(g => g?.Roles.Add(new EnemyGroupRole()));
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
                    group.PropertyChanged += (_, args) =>
                    {
                        RefreshSpawningOptions();
                        if (args.PropertyName == nameof(EnemyGroupBlock.Difficulty)) RefreshRoleOptions();
                        RefreshGroupSearch();
                    };
            }
            RefreshSpawningOptions();
            RefreshRoleOptions();
            RefreshGroupSearch();
        };
        Population.RoleDatas.CollectionChanged += (_, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (EnemyRoleData role in e.NewItems)
                    role.PropertyChanged += (_, _) => RefreshPopulationSearch();
            }
            RefreshSpawningOptions();
            RefreshRoleOptions();
            RefreshPopulationSearch();
        };
        foreach (var role in Population.RoleDatas)
        {
            role.PropertyChanged += (_, _) => RefreshSpawningOptions();
        }
        RefreshSpawningOptions();
        RefreshPopulationSearch();
        RefreshGroupSearch();
    }

    private void LoadVanillaDatablocks()
    {
        string dir = AlarmService.GetDatablocksDirectory();
        string populationPath = Path.Combine(dir, "EnemyPopulationDataBlock.json");
        if (File.Exists(populationPath))
        {
            try
            {
                if (JsonNode.Parse(File.ReadAllText(populationPath)) is JsonObject obj &&
                    obj["Blocks"] is JsonArray blocks)
                {
                    foreach (var blockNode in blocks.OfType<JsonObject>())
                    {
                        var population = new EnemyPopulationBlock();
                        if (blockNode["RoleDatas"] is JsonArray roles)
                        {
                            foreach (var roleNode in roles.OfType<JsonObject>())
                                population.RoleDatas.Add(new EnemyRoleData
                                {
                                    Role = roleNode["Role"]?.GetValue<string>() ?? string.Empty,
                                    Difficulty = roleNode["Difficulty"]?.GetValue<string>() ?? "Easy",
                                    Enemy = roleNode["Enemy"]?.GetValue<uint>() ?? 0,
                                    Cost = roleNode["Cost"]?.GetValue<double>() ?? 1,
                                    Weight = roleNode["Weight"]?.GetValue<double>() ?? 1
                                });
                        }
                        PopulationBlocks.Add(population);
                        break;
                    }
                }
            }
            catch { }
        }

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
                            Type = "genericType",
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
        EnemySpawningService.RefreshOptions(Groups, Population.RoleDatas, Enemies);
        RefreshRoleOptions();
    }

    private void RefreshPopulationSearch()
    {
        PopulationSearchResults.Clear();
        var query = PopulationSearchText?.Trim() ?? string.Empty;
        foreach (var role in Population.RoleDatas)
        {
            if (string.IsNullOrEmpty(query) || MatchesPopulationRole(role, query))
                PopulationSearchResults.Add(role);
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
        foreach (var group in Groups)
        {
            group.AvailableRoles.Clear();
            var matchingRoles = Population.RoleDatas
                         .Where(r => string.Equals(r.Difficulty, group.Difficulty, StringComparison.OrdinalIgnoreCase))
                         .Select(r => r.Role)
                         .Where(r => !string.IsNullOrWhiteSpace(r))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToList();
            foreach (var role in matchingRoles)
            {
                group.AvailableRoles.Add(role);
            }
            foreach (var groupRole in group.Roles)
            {
                groupRole.AvailableRoles.Clear();
                foreach (var role in matchingRoles) groupRole.AvailableRoles.Add(role);
                if (!matchingRoles.Contains(groupRole.Role, StringComparer.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(groupRole.Role))
                {
                    groupRole.AvailableRoles.Add(groupRole.Role);
                }
            }
        }
        foreach (var populationRole in Population.RoleDatas)
        {
            populationRole.EnemyEntries.Clear();
            foreach (var enemy in Enemies) populationRole.EnemyEntries.Add(enemy);
            populationRole.SelectedEnemy = Enemies.FirstOrDefault(e => e.PersistentId == populationRole.Enemy);
        }
        OnPropertyChanged(nameof(Groups));
    }
}
