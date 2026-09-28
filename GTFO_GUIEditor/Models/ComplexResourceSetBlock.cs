using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class ComplexResourceSetBlock : INotifyPropertyChanged
{
    private string _name = "New Complex Resource Set";
    private bool _internalEnabled = true;
    private uint _persistentId;
    private ComplexType _complexType = ComplexType.Mining;
    private SubComplexType _primareSubComplexUsed = SubComplexType.DigSite;
    private BundleNameType _bundleName = BundleNameType.Complex_Mining;
    private bool _randomizeGeomorphOrder;
    private LevelGenConfig _levelGenConfig = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("internalEnabled")]
    public bool InternalEnabled
    {
        get => _internalEnabled;
        set => SetField(ref _internalEnabled, value);
    }

    [JsonPropertyName("persistentID")]
    public uint PersistentId
    {
        get => _persistentId;
        set
        {
            if (SetField(ref _persistentId, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("ComplexType")]
    public ComplexType ComplexType
    {
        get => _complexType;
        set => SetField(ref _complexType, value);
    }

    [JsonPropertyName("PrimareSubComplexUsed")]
    public SubComplexType PrimareSubComplexUsed
    {
        get => _primareSubComplexUsed;
        set => SetField(ref _primareSubComplexUsed, value);
    }

    [JsonPropertyName("BundleName")]
    public BundleNameType BundleName
    {
        get => _bundleName;
        set => SetField(ref _bundleName, value);
    }

    [JsonPropertyName("RandomizeGeomorphOrder")]
    public bool RandomizeGeomorphOrder
    {
        get => _randomizeGeomorphOrder;
        set => SetField(ref _randomizeGeomorphOrder, value);
    }

    [JsonPropertyName("LevelGenConfig")]
    public LevelGenConfig LevelGenConfig
    {
        get => _levelGenConfig;
        set => SetField(ref _levelGenConfig, value);
    }

    [JsonPropertyName("GeomorphTiles_1x1")]
    public List<PrefabItem> GeomorphTiles_1x1 { get; set; } = new();

    [JsonPropertyName("GeomorphTiles_2x1")]
    public List<PrefabItem> GeomorphTiles_2x1 { get; set; } = new();

    [JsonPropertyName("GeomorphTiles_2x2")]
    public List<PrefabItem> GeomorphTiles_2x2 { get; set; } = new();

    [JsonPropertyName("CustomGeomorphs_Challenge_1x1")]
    public List<PrefabItem> CustomGeomorphs_Challenge_1x1 { get; set; } = new();

    [JsonPropertyName("CustomGeomorphs_Exit_1x1")]
    public List<PrefabItem> CustomGeomorphs_Exit_1x1 { get; set; } = new();

    [JsonPropertyName("CustomGeomorphs_Objective_1x1")]
    public List<PrefabItem> CustomGeomorphs_Objective_1x1 { get; set; } = new();

    [JsonPropertyName("ElevatorShafts_1x1")]
    public List<PrefabItem> ElevatorShafts_1x1 { get; set; } = new();

    [JsonPropertyName("Ladders_05m")]
    public List<PrefabItem> Ladders_05m { get; set; } = new();

    [JsonPropertyName("Ladders_1m")]
    public List<PrefabItem> Ladders_1m { get; set; } = new();

    [JsonPropertyName("Ladders_2m")]
    public List<PrefabItem> Ladders_2m { get; set; } = new();

    [JsonPropertyName("Ladders_4m")]
    public List<PrefabItem> Ladders_4m { get; set; } = new();

    [JsonPropertyName("Ladders_Bottom")]
    public List<PrefabItem> Ladders_Bottom { get; set; } = new();

    [JsonPropertyName("Ladders_Top")]
    public List<PrefabItem> Ladders_Top { get; set; } = new();

    [JsonPropertyName("StraightPlugsNoGates")]
    public List<PrefabItem> StraightPlugsNoGates { get; set; } = new();

    [JsonPropertyName("StraightPlugsWithGates")]
    public List<PrefabItem> StraightPlugsWithGates { get; set; } = new();

    [JsonPropertyName("SingleDropPlugsNoGates")]
    public List<PrefabItem> SingleDropPlugsNoGates { get; set; } = new();

    [JsonPropertyName("SingleDropPlugsWithGates")]
    public List<PrefabItem> SingleDropPlugsWithGates { get; set; } = new();

    [JsonPropertyName("DoubleDropPlugsNoGates")]
    public List<PrefabItem> DoubleDropPlugsNoGates { get; set; } = new();

    [JsonPropertyName("DoubleDropPlugsWithGates")]
    public List<PrefabItem> DoubleDropPlugsWithGates { get; set; } = new();

    [JsonPropertyName("PlugCaps")]
    public List<PrefabItem> PlugCaps { get; set; } = new();

    [JsonPropertyName("SmallApexGates")]
    public List<PrefabItem> SmallApexGates { get; set; } = new();

    [JsonPropertyName("SmallBulkheadGates")]
    public List<PrefabItem> SmallBulkheadGates { get; set; } = new();

    [JsonPropertyName("SmallDestroyedCaps")]
    public List<PrefabItem> SmallDestroyedCaps { get; set; } = new();

    [JsonPropertyName("SmallMainPathBulkheadGates")]
    public List<PrefabItem> SmallMainPathBulkheadGates { get; set; } = new();

    [JsonPropertyName("SmallSecurityGates")]
    public List<PrefabItem> SmallSecurityGates { get; set; } = new();

    [JsonPropertyName("SmallWallAndDestroyedCaps")]
    public List<PrefabItem> SmallWallAndDestroyedCaps { get; set; } = new();

    [JsonPropertyName("SmallWallCaps")]
    public List<PrefabItem> SmallWallCaps { get; set; } = new();

    [JsonPropertyName("SmallWeakGates")]
    public List<PrefabItem> SmallWeakGates { get; set; } = new();

    [JsonPropertyName("MediumApexGates")]
    public List<PrefabItem> MediumApexGates { get; set; } = new();

    [JsonPropertyName("MediumBulkheadGates")]
    public List<PrefabItem> MediumBulkheadGates { get; set; } = new();

    [JsonPropertyName("MediumDestroyedCaps")]
    public List<PrefabItem> MediumDestroyedCaps { get; set; } = new();

    [JsonPropertyName("MediumMainPathBulkheadGates")]
    public List<PrefabItem> MediumMainPathBulkheadGates { get; set; } = new();

    [JsonPropertyName("MediumSecurityGates")]
    public List<PrefabItem> MediumSecurityGates { get; set; } = new();

    [JsonPropertyName("MediumWallAndDestroyedCaps")]
    public List<PrefabItem> MediumWallAndDestroyedCaps { get; set; } = new();

    [JsonPropertyName("MediumWallCaps")]
    public List<PrefabItem> MediumWallCaps { get; set; } = new();

    [JsonPropertyName("MediumWeakGates")]
    public List<PrefabItem> MediumWeakGates { get; set; } = new();

    [JsonPropertyName("LargeApexGates")]
    public List<PrefabItem> LargeApexGates { get; set; } = new();

    [JsonPropertyName("LargeBulkheadGates")]
    public List<PrefabItem> LargeBulkheadGates { get; set; } = new();

    [JsonPropertyName("LargeDestroyedCaps")]
    public List<PrefabItem> LargeDestroyedCaps { get; set; } = new();

    [JsonPropertyName("LargeMainPathBulkheadGates")]
    public List<PrefabItem> LargeMainPathBulkheadGates { get; set; } = new();

    [JsonPropertyName("LargeSecurityGates")]
    public List<PrefabItem> LargeSecurityGates { get; set; } = new();

    [JsonPropertyName("LargeWallAndDestroyedCaps")]
    public List<PrefabItem> LargeWallAndDestroyedCaps { get; set; } = new();

    [JsonPropertyName("LargeWallCaps")]
    public List<PrefabItem> LargeWallCaps { get; set; } = new();

    [JsonPropertyName("LargeWeakGates")]
    public List<PrefabItem> LargeWeakGates { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    public string DisplayText => $"[{PersistentId}] {Name}";

    public static readonly string[] CategoryNames =
    {
        nameof(GeomorphTiles_1x1),
        nameof(GeomorphTiles_2x1),
        nameof(GeomorphTiles_2x2),
        nameof(CustomGeomorphs_Challenge_1x1),
        nameof(CustomGeomorphs_Exit_1x1),
        nameof(CustomGeomorphs_Objective_1x1),
        nameof(ElevatorShafts_1x1),
        nameof(Ladders_05m),
        nameof(Ladders_1m),
        nameof(Ladders_2m),
        nameof(Ladders_4m),
        nameof(Ladders_Bottom),
        nameof(Ladders_Top),
        nameof(StraightPlugsNoGates),
        nameof(StraightPlugsWithGates),
        nameof(SingleDropPlugsNoGates),
        nameof(SingleDropPlugsWithGates),
        nameof(DoubleDropPlugsNoGates),
        nameof(DoubleDropPlugsWithGates),
        nameof(PlugCaps),
        nameof(SmallApexGates),
        nameof(SmallBulkheadGates),
        nameof(SmallDestroyedCaps),
        nameof(SmallMainPathBulkheadGates),
        nameof(SmallSecurityGates),
        nameof(SmallWallAndDestroyedCaps),
        nameof(SmallWallCaps),
        nameof(SmallWeakGates),
        nameof(MediumApexGates),
        nameof(MediumBulkheadGates),
        nameof(MediumDestroyedCaps),
        nameof(MediumMainPathBulkheadGates),
        nameof(MediumSecurityGates),
        nameof(MediumWallAndDestroyedCaps),
        nameof(MediumWallCaps),
        nameof(MediumWeakGates),
        nameof(LargeApexGates),
        nameof(LargeBulkheadGates),
        nameof(LargeDestroyedCaps),
        nameof(LargeMainPathBulkheadGates),
        nameof(LargeSecurityGates),
        nameof(LargeWallAndDestroyedCaps),
        nameof(LargeWallCaps),
        nameof(LargeWeakGates)
    };

    public List<PrefabItem> GetListByCategory(string categoryName)
    {
        return categoryName switch
        {
            nameof(GeomorphTiles_1x1) => GeomorphTiles_1x1,
            nameof(GeomorphTiles_2x1) => GeomorphTiles_2x1,
            nameof(GeomorphTiles_2x2) => GeomorphTiles_2x2,
            nameof(CustomGeomorphs_Challenge_1x1) => CustomGeomorphs_Challenge_1x1,
            nameof(CustomGeomorphs_Exit_1x1) => CustomGeomorphs_Exit_1x1,
            nameof(CustomGeomorphs_Objective_1x1) => CustomGeomorphs_Objective_1x1,
            nameof(ElevatorShafts_1x1) => ElevatorShafts_1x1,
            nameof(Ladders_05m) => Ladders_05m,
            nameof(Ladders_1m) => Ladders_1m,
            nameof(Ladders_2m) => Ladders_2m,
            nameof(Ladders_4m) => Ladders_4m,
            nameof(Ladders_Bottom) => Ladders_Bottom,
            nameof(Ladders_Top) => Ladders_Top,
            nameof(StraightPlugsNoGates) => StraightPlugsNoGates,
            nameof(StraightPlugsWithGates) => StraightPlugsWithGates,
            nameof(SingleDropPlugsNoGates) => SingleDropPlugsNoGates,
            nameof(SingleDropPlugsWithGates) => SingleDropPlugsWithGates,
            nameof(DoubleDropPlugsNoGates) => DoubleDropPlugsNoGates,
            nameof(DoubleDropPlugsWithGates) => DoubleDropPlugsWithGates,
            nameof(PlugCaps) => PlugCaps,
            nameof(SmallApexGates) => SmallApexGates,
            nameof(SmallBulkheadGates) => SmallBulkheadGates,
            nameof(SmallDestroyedCaps) => SmallDestroyedCaps,
            nameof(SmallMainPathBulkheadGates) => SmallMainPathBulkheadGates,
            nameof(SmallSecurityGates) => SmallSecurityGates,
            nameof(SmallWallAndDestroyedCaps) => SmallWallAndDestroyedCaps,
            nameof(SmallWallCaps) => SmallWallCaps,
            nameof(SmallWeakGates) => SmallWeakGates,
            nameof(MediumApexGates) => MediumApexGates,
            nameof(MediumBulkheadGates) => MediumBulkheadGates,
            nameof(MediumDestroyedCaps) => MediumDestroyedCaps,
            nameof(MediumMainPathBulkheadGates) => MediumMainPathBulkheadGates,
            nameof(MediumSecurityGates) => MediumSecurityGates,
            nameof(MediumWallAndDestroyedCaps) => MediumWallAndDestroyedCaps,
            nameof(MediumWallCaps) => MediumWallCaps,
            nameof(MediumWeakGates) => MediumWeakGates,
            nameof(LargeApexGates) => LargeApexGates,
            nameof(LargeBulkheadGates) => LargeBulkheadGates,
            nameof(LargeDestroyedCaps) => LargeDestroyedCaps,
            nameof(LargeMainPathBulkheadGates) => LargeMainPathBulkheadGates,
            nameof(LargeSecurityGates) => LargeSecurityGates,
            nameof(LargeWallAndDestroyedCaps) => LargeWallAndDestroyedCaps,
            nameof(LargeWallCaps) => LargeWallCaps,
            nameof(LargeWeakGates) => LargeWeakGates,
            _ => GeomorphTiles_1x1
        };
    }

    private static List<PrefabItem> CloneList(List<PrefabItem> list)
    {
        return list.Select(item => item.Clone()).ToList();
    }

    public ComplexResourceSetBlock Clone()
    {
        var clone = new ComplexResourceSetBlock
        {
            Name = Name,
            InternalEnabled = InternalEnabled,
            PersistentId = PersistentId,
            ComplexType = ComplexType,
            PrimareSubComplexUsed = PrimareSubComplexUsed,
            BundleName = BundleName,
            RandomizeGeomorphOrder = RandomizeGeomorphOrder,
            LevelGenConfig = LevelGenConfig.Clone(),

            GeomorphTiles_1x1 = CloneList(GeomorphTiles_1x1),
            GeomorphTiles_2x1 = CloneList(GeomorphTiles_2x1),
            GeomorphTiles_2x2 = CloneList(GeomorphTiles_2x2),
            CustomGeomorphs_Challenge_1x1 = CloneList(CustomGeomorphs_Challenge_1x1),
            CustomGeomorphs_Exit_1x1 = CloneList(CustomGeomorphs_Exit_1x1),
            CustomGeomorphs_Objective_1x1 = CloneList(CustomGeomorphs_Objective_1x1),
            ElevatorShafts_1x1 = CloneList(ElevatorShafts_1x1),
            Ladders_05m = CloneList(Ladders_05m),
            Ladders_1m = CloneList(Ladders_1m),
            Ladders_2m = CloneList(Ladders_2m),
            Ladders_4m = CloneList(Ladders_4m),
            Ladders_Bottom = CloneList(Ladders_Bottom),
            Ladders_Top = CloneList(Ladders_Top),
            StraightPlugsNoGates = CloneList(StraightPlugsNoGates),
            StraightPlugsWithGates = CloneList(StraightPlugsWithGates),
            SingleDropPlugsNoGates = CloneList(SingleDropPlugsNoGates),
            SingleDropPlugsWithGates = CloneList(SingleDropPlugsWithGates),
            DoubleDropPlugsNoGates = CloneList(DoubleDropPlugsNoGates),
            DoubleDropPlugsWithGates = CloneList(DoubleDropPlugsWithGates),
            PlugCaps = CloneList(PlugCaps),
            SmallApexGates = CloneList(SmallApexGates),
            SmallBulkheadGates = CloneList(SmallBulkheadGates),
            SmallDestroyedCaps = CloneList(SmallDestroyedCaps),
            SmallMainPathBulkheadGates = CloneList(SmallMainPathBulkheadGates),
            SmallSecurityGates = CloneList(SmallSecurityGates),
            SmallWallAndDestroyedCaps = CloneList(SmallWallAndDestroyedCaps),
            SmallWallCaps = CloneList(SmallWallCaps),
            SmallWeakGates = CloneList(SmallWeakGates),
            MediumApexGates = CloneList(MediumApexGates),
            MediumBulkheadGates = CloneList(MediumBulkheadGates),
            MediumDestroyedCaps = CloneList(MediumDestroyedCaps),
            MediumMainPathBulkheadGates = CloneList(MediumMainPathBulkheadGates),
            MediumSecurityGates = CloneList(MediumSecurityGates),
            MediumWallAndDestroyedCaps = CloneList(MediumWallAndDestroyedCaps),
            MediumWallCaps = CloneList(MediumWallCaps),
            MediumWeakGates = CloneList(MediumWeakGates),
            LargeApexGates = CloneList(LargeApexGates),
            LargeBulkheadGates = CloneList(LargeBulkheadGates),
            LargeDestroyedCaps = CloneList(LargeDestroyedCaps),
            LargeMainPathBulkheadGates = CloneList(LargeMainPathBulkheadGates),
            LargeSecurityGates = CloneList(LargeSecurityGates),
            LargeWallAndDestroyedCaps = CloneList(LargeWallAndDestroyedCaps),
            LargeWallCaps = CloneList(LargeWallCaps),
            LargeWeakGates = CloneList(LargeWeakGates)
        };

        if (ExtensionData != null)
        {
            clone.ExtensionData = new Dictionary<string, JsonElement>(ExtensionData);
        }

        return clone;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
