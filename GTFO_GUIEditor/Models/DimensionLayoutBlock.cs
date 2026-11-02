using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class DimensionLayoutBlock : INotifyPropertyChanged
{
    private string _dimensionIndex = "Dimension_1";
    private string _name = "Dimension 1";
    private bool _internalEnabled = true;
    private ComplexType _complexType = ComplexType.Mining;
    private uint _complexResourceSet;
    private string _dimensionGeomorph = string.Empty;
    private uint _levelLayoutData;
    private string _tileCategory = "Dimension Geomorph";
    private LevelLayoutGridState? _gridState;

    // General Section
    private bool _forbidWaveSpawning = false;
    private DimensionLinkedToLayer _linkedToLayer = DimensionLinkedToLayer.Main;
    private DimensionObjectiveType _objectiveType = DimensionObjectiveType.Ignore;

    // Warp Section
    private uint _soundEventOnWarpTo = 1998147319;
    private bool _disableVFXEventOnWarp = false;
    private bool _forbidCarryItemWarps = false;
    private bool _leaveDeployablesOnWarp = false;

    // Environment Section
    private uint _dimensionFogData = 21;
    private double _environmentWetness = 0.0;
    private LightColor _dustColor = new() { A = 0.5686275, R = 0.496, G = 0.496, B = 0.496 };
    private double _dustAlphaBoost = 0.0;
    private double _dustTurbulence = 1.0;

    // Static Settings Section
    private bool _isStaticDimension = true;
    private int _staticZoneSeed = 0;
    private int _staticMarkerSeed = 0;
    private int _staticAliasOverride = -1;
    private uint _staticConsumableDistributionInZone = 0;
    private ObservableCollection<BigPickupDistributionItem> _staticBigPickupDistributionInZone = new();
    private int _staticHealthUses = 0;
    private int _staticToolUses = 0;
    private int _staticAmmoUses = 0;
    private int _staticDisinfectUses = 0;
    private ObservableCollection<TerminalPlacement> _staticTerminalPlacements = new();

    // Atmosphere Section
    private bool _isOutside = true;
    private double _lightAzimuth = -92.9;
    private double _lightElevation = 30.0;
    private double _lightIntensity = 1.0;
    private double _ambientIntensity = 1.0;
    private double _reflectionsIntensity = 1.0;
    private double _godrayRange = 8000.0;
    private double _godrayExponent = 1.0;
    private LightColor _atmosphereLightColor = new() { A = 1.0, R = 0.146270916, G = 0.1618043, B = 0.1792453 };
    private LightColor _atmospherePlanetAlbedo = new() { A = 1.0, R = 0.2, G = 0.0627451, B = 0.0 };
    private double _atmosphereDensity = 1.0;
    private double _exposure = 10.0;

    // Clouds Section
    private DimensionCloudsType _cloudsData = DimensionCloudsType.Anvils;
    private double _cloudsCoverage = 0.5;
    private double _cloudsDensity = 0.5;
    private double _cloudsSharpness = 0.0;
    private double _cloudsShadowOpacity = 1.0;
    private double _cloudsTimescale = 0.0;
    private double _cloudsCrawling = 0.1;
    private double _cloudsFade = 0.1;
    private bool _sandstorm = false;
    private double _sandstormEdgeA = 0.4;
    private double _sandstormEdgeB = 0.6;
    private double _sandstormMinFog = 0.01;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("DimensionIndex")]
    public string DimensionIndex
    {
        get => _dimensionIndex;
        set
        {
            if (SetField(ref _dimensionIndex, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

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

    [JsonPropertyName("ComplexType")]
    public ComplexType ComplexType
    {
        get => _complexType;
        set => SetField(ref _complexType, value);
    }

    [JsonPropertyName("ComplexResourceSet")]
    public uint ComplexResourceSet
    {
        get => _complexResourceSet;
        set
        {
            if (SetField(ref _complexResourceSet, value))
            {
                OnPropertyChanged(nameof(DimensionResourceSetID));
            }
        }
    }

    [JsonPropertyName("DimensionResourceSetID")]
    public uint DimensionResourceSetID
    {
        get => _complexResourceSet;
        set
        {
            if (SetField(ref _complexResourceSet, value))
            {
                OnPropertyChanged(nameof(ComplexResourceSet));
            }
        }
    }

    [JsonPropertyName("DimensionGeomorph")]
    public string DimensionGeomorph
    {
        get => _dimensionGeomorph;
        set => SetField(ref _dimensionGeomorph, value);
    }

    [JsonPropertyName("LevelLayoutData")]
    public uint LevelLayoutData
    {
        get => _levelLayoutData;
        set => SetField(ref _levelLayoutData, value);
    }

    [JsonPropertyName("TileCategory")]
    public string TileCategory
    {
        get => _tileCategory;
        set => SetField(ref _tileCategory, value);
    }

    // General Properties
    [JsonPropertyName("ForbidWaveSpawning")]
    public bool ForbidWaveSpawning
    {
        get => _forbidWaveSpawning;
        set => SetField(ref _forbidWaveSpawning, value);
    }

    [JsonPropertyName("LinkedToLayer")]
    public DimensionLinkedToLayer LinkedToLayer
    {
        get => _linkedToLayer;
        set => SetField(ref _linkedToLayer, value);
    }

    [JsonPropertyName("ObjectiveType")]
    public DimensionObjectiveType ObjectiveType
    {
        get => _objectiveType;
        set => SetField(ref _objectiveType, value);
    }

    // Warp Properties
    [JsonPropertyName("SoundEventOnWarpTo")]
    public uint SoundEventOnWarpTo
    {
        get => _soundEventOnWarpTo;
        set => SetField(ref _soundEventOnWarpTo, value);
    }

    [JsonPropertyName("DisableVFXEventOnWarp")]
    public bool DisableVFXEventOnWarp
    {
        get => _disableVFXEventOnWarp;
        set => SetField(ref _disableVFXEventOnWarp, value);
    }

    [JsonPropertyName("ForbidCarryItemWarps")]
    public bool ForbidCarryItemWarps
    {
        get => _forbidCarryItemWarps;
        set => SetField(ref _forbidCarryItemWarps, value);
    }

    [JsonPropertyName("LeaveDeployablesOnWarp")]
    public bool LeaveDeployablesOnWarp
    {
        get => _leaveDeployablesOnWarp;
        set => SetField(ref _leaveDeployablesOnWarp, value);
    }

    // Environment Properties
    [JsonPropertyName("DimensionFogData")]
    public uint DimensionFogData
    {
        get => _dimensionFogData;
        set => SetField(ref _dimensionFogData, value);
    }

    [JsonPropertyName("EnvironmentWetness")]
    public double EnvironmentWetness
    {
        get => _environmentWetness;
        set => SetField(ref _environmentWetness, value);
    }

    [JsonPropertyName("DustColor")]
    public LightColor DustColor
    {
        get => _dustColor;
        set => SetField(ref _dustColor, value ?? new LightColor());
    }

    [JsonPropertyName("DustAlphaBoost")]
    public double DustAlphaBoost
    {
        get => _dustAlphaBoost;
        set => SetField(ref _dustAlphaBoost, value);
    }

    [JsonPropertyName("DustTurbulence")]
    public double DustTurbulence
    {
        get => _dustTurbulence;
        set => SetField(ref _dustTurbulence, value);
    }

    // Static Settings Properties
    [JsonPropertyName("IsStaticDimension")]
    public bool IsStaticDimension
    {
        get => _isStaticDimension;
        set => SetField(ref _isStaticDimension, value);
    }

    [JsonPropertyName("StaticZoneSeed")]
    public int StaticZoneSeed
    {
        get => _staticZoneSeed;
        set => SetField(ref _staticZoneSeed, value);
    }

    [JsonPropertyName("StaticMarkerSeed")]
    public int StaticMarkerSeed
    {
        get => _staticMarkerSeed;
        set => SetField(ref _staticMarkerSeed, value);
    }

    [JsonPropertyName("StaticAliasOverride")]
    public int StaticAliasOverride
    {
        get => _staticAliasOverride;
        set => SetField(ref _staticAliasOverride, value);
    }

    [JsonPropertyName("StaticConsumableDistributionInZone")]
    public uint StaticConsumableDistributionInZone
    {
        get => _staticConsumableDistributionInZone;
        set => SetField(ref _staticConsumableDistributionInZone, value);
    }

    [JsonPropertyName("StaticBigPickupDistributionInZone")]
    public ObservableCollection<BigPickupDistributionItem> StaticBigPickupDistributionInZone
    {
        get => _staticBigPickupDistributionInZone;
        set => SetField(ref _staticBigPickupDistributionInZone, value ?? new());
    }

    [JsonPropertyName("StaticHealthUses")]
    public int StaticHealthUses
    {
        get => _staticHealthUses;
        set => SetField(ref _staticHealthUses, value);
    }

    [JsonPropertyName("StaticToolUses")]
    public int StaticToolUses
    {
        get => _staticToolUses;
        set => SetField(ref _staticToolUses, value);
    }

    [JsonPropertyName("StaticAmmoUses")]
    public int StaticAmmoUses
    {
        get => _staticAmmoUses;
        set => SetField(ref _staticAmmoUses, value);
    }

    [JsonPropertyName("StaticDisinfectUses")]
    public int StaticDisinfectUses
    {
        get => _staticDisinfectUses;
        set => SetField(ref _staticDisinfectUses, value);
    }

    [JsonPropertyName("StaticTerminalPlacements")]
    public ObservableCollection<TerminalPlacement> StaticTerminalPlacements
    {
        get => _staticTerminalPlacements;
        set => SetField(ref _staticTerminalPlacements, value ?? new());
    }

    // Atmosphere Properties
    [JsonPropertyName("IsOutside")]
    public bool IsOutside
    {
        get => _isOutside;
        set => SetField(ref _isOutside, value);
    }

    [JsonPropertyName("LightAzimuth")]
    public double LightAzimuth
    {
        get => _lightAzimuth;
        set => SetField(ref _lightAzimuth, value);
    }

    [JsonPropertyName("LightElevation")]
    public double LightElevation
    {
        get => _lightElevation;
        set => SetField(ref _lightElevation, value);
    }

    [JsonPropertyName("LightIntensity")]
    public double LightIntensity
    {
        get => _lightIntensity;
        set => SetField(ref _lightIntensity, value);
    }

    [JsonPropertyName("AmbientIntensity")]
    public double AmbientIntensity
    {
        get => _ambientIntensity;
        set => SetField(ref _ambientIntensity, value);
    }

    [JsonPropertyName("ReflectionsIntensity")]
    public double ReflectionsIntensity
    {
        get => _reflectionsIntensity;
        set => SetField(ref _reflectionsIntensity, value);
    }

    [JsonPropertyName("GodrayRange")]
    public double GodrayRange
    {
        get => _godrayRange;
        set => SetField(ref _godrayRange, value);
    }

    [JsonPropertyName("GodrayExponent")]
    public double GodrayExponent
    {
        get => _godrayExponent;
        set => SetField(ref _godrayExponent, value);
    }

    [JsonPropertyName("AtmosphereLightColor")]
    public LightColor AtmosphereLightColor
    {
        get => _atmosphereLightColor;
        set => SetField(ref _atmosphereLightColor, value ?? new LightColor());
    }

    [JsonPropertyName("AtmospherePlanetAlbedo")]
    public LightColor AtmospherePlanetAlbedo
    {
        get => _atmospherePlanetAlbedo;
        set => SetField(ref _atmospherePlanetAlbedo, value ?? new LightColor());
    }

    [JsonPropertyName("AtmosphereDensity")]
    public double AtmosphereDensity
    {
        get => _atmosphereDensity;
        set => SetField(ref _atmosphereDensity, value);
    }

    [JsonPropertyName("Exposure")]
    public double Exposure
    {
        get => _exposure;
        set => SetField(ref _exposure, value);
    }

    // Clouds Properties
    [JsonPropertyName("CloudsData")]
    public DimensionCloudsType CloudsData
    {
        get => _cloudsData;
        set => SetField(ref _cloudsData, value);
    }

    [JsonPropertyName("CloudsCoverage")]
    public double CloudsCoverage
    {
        get => _cloudsCoverage;
        set => SetField(ref _cloudsCoverage, value);
    }

    [JsonPropertyName("CloudsDensity")]
    public double CloudsDensity
    {
        get => _cloudsDensity;
        set => SetField(ref _cloudsDensity, value);
    }

    [JsonPropertyName("CloudsSharpness")]
    public double CloudsSharpness
    {
        get => _cloudsSharpness;
        set => SetField(ref _cloudsSharpness, value);
    }

    [JsonPropertyName("CloudsShadowOpacity")]
    public double CloudsShadowOpacity
    {
        get => _cloudsShadowOpacity;
        set => SetField(ref _cloudsShadowOpacity, value);
    }

    [JsonPropertyName("CloudsTimescale")]
    public double CloudsTimescale
    {
        get => _cloudsTimescale;
        set => SetField(ref _cloudsTimescale, value);
    }

    [JsonPropertyName("CloudsCrawling")]
    public double CloudsCrawling
    {
        get => _cloudsCrawling;
        set => SetField(ref _cloudsCrawling, value);
    }

    [JsonPropertyName("CloudsFade")]
    public double CloudsFade
    {
        get => _cloudsFade;
        set => SetField(ref _cloudsFade, value);
    }

    [JsonPropertyName("Sandstorm")]
    public bool Sandstorm
    {
        get => _sandstorm;
        set => SetField(ref _sandstorm, value);
    }

    [JsonPropertyName("SandstormEdgeA")]
    public double SandstormEdgeA
    {
        get => _sandstormEdgeA;
        set => SetField(ref _sandstormEdgeA, value);
    }

    [JsonPropertyName("SandstormEdgeB")]
    public double SandstormEdgeB
    {
        get => _sandstormEdgeB;
        set => SetField(ref _sandstormEdgeB, value);
    }

    [JsonPropertyName("SandstormMinFog")]
    public double SandstormMinFog
    {
        get => _sandstormMinFog;
        set => SetField(ref _sandstormMinFog, value);
    }

    [JsonPropertyName("gridState")]
    public LevelLayoutGridStateData? GridStateData
    {
        get => _gridState?.ExportStateData();
        set
        {
            if (value != null)
            {
                GridState.ImportStateData(value);
            }
        }
    }

    [JsonIgnore]
    public LevelLayoutGridState GridState
    {
        get
        {
            if (_gridState == null)
            {
                _gridState = new LevelLayoutGridState();
                if (!string.IsNullOrWhiteSpace(DimensionGeomorph))
                {
                    _gridState.InitializeStartingTile(DimensionGeomorph, 0, 0);
                }
            }
            return _gridState;
        }
        set => SetField(ref _gridState, value);
    }

    [JsonIgnore]
    public string DisplayText => string.IsNullOrWhiteSpace(Name)
        ? DimensionIndex
        : Name;

    public DimensionLayoutBlock Clone()
    {
        var clone = new DimensionLayoutBlock
        {
            DimensionIndex = DimensionIndex,
            Name = Name,
            InternalEnabled = InternalEnabled,
            ComplexType = ComplexType,
            ComplexResourceSet = ComplexResourceSet,
            DimensionGeomorph = DimensionGeomorph,
            LevelLayoutData = LevelLayoutData,
            TileCategory = TileCategory,

            ForbidWaveSpawning = ForbidWaveSpawning,
            LinkedToLayer = LinkedToLayer,
            ObjectiveType = ObjectiveType,

            SoundEventOnWarpTo = SoundEventOnWarpTo,
            DisableVFXEventOnWarp = DisableVFXEventOnWarp,
            ForbidCarryItemWarps = ForbidCarryItemWarps,
            LeaveDeployablesOnWarp = LeaveDeployablesOnWarp,

            DimensionFogData = DimensionFogData,
            EnvironmentWetness = EnvironmentWetness,
            DustColor = DustColor?.Clone() ?? new LightColor(),
            DustAlphaBoost = DustAlphaBoost,
            DustTurbulence = DustTurbulence,

            IsStaticDimension = IsStaticDimension,
            StaticZoneSeed = StaticZoneSeed,
            StaticMarkerSeed = StaticMarkerSeed,
            StaticAliasOverride = StaticAliasOverride,
            StaticConsumableDistributionInZone = StaticConsumableDistributionInZone,
            StaticBigPickupDistributionInZone = new ObservableCollection<BigPickupDistributionItem>(
                StaticBigPickupDistributionInZone.Select(b => b.Clone())),
            StaticHealthUses = StaticHealthUses,
            StaticToolUses = StaticToolUses,
            StaticAmmoUses = StaticAmmoUses,
            StaticDisinfectUses = StaticDisinfectUses,
            StaticTerminalPlacements = new ObservableCollection<TerminalPlacement>(
                StaticTerminalPlacements.Select(t => t.Clone())),

            IsOutside = IsOutside,
            LightAzimuth = LightAzimuth,
            LightElevation = LightElevation,
            LightIntensity = LightIntensity,
            AmbientIntensity = AmbientIntensity,
            ReflectionsIntensity = ReflectionsIntensity,
            GodrayRange = GodrayRange,
            GodrayExponent = GodrayExponent,
            AtmosphereLightColor = AtmosphereLightColor?.Clone() ?? new LightColor(),
            AtmospherePlanetAlbedo = AtmospherePlanetAlbedo?.Clone() ?? new LightColor(),
            AtmosphereDensity = AtmosphereDensity,
            Exposure = Exposure,

            CloudsData = CloudsData,
            CloudsCoverage = CloudsCoverage,
            CloudsDensity = CloudsDensity,
            CloudsSharpness = CloudsSharpness,
            CloudsShadowOpacity = CloudsShadowOpacity,
            CloudsTimescale = CloudsTimescale,
            CloudsCrawling = CloudsCrawling,
            CloudsFade = CloudsFade,
            Sandstorm = Sandstorm,
            SandstormEdgeA = SandstormEdgeA,
            SandstormEdgeB = SandstormEdgeB,
            SandstormMinFog = SandstormMinFog
        };

        if (_gridState != null)
        {
            clone.GridState = _gridState.Clone();
        }
        return clone;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
