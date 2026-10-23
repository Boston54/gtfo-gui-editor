using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.Models;

public class ZoneSettings : INotifyPropertyChanged
{
    private SectorType _sector = SectorType.Main;
    private int _zoneIndex;
    private uint _lighting;
    private uint _consumableDistributionInZone;
    private int _medKitUses;
    private int _ammoKitUses;
    private int _toolKitUses;
    private int _disinfectKitUses;
    private bool _containsDC;
    private bool _dcOpensSecondary;
    private bool _dcOpensOverload;
    private ObservableCollection<string> _dcOpenedMainDoorIds = new();
    private bool _containsGenCluster;
    private int _powerCells;
    private int _fogTurbines;

    // Seeds
    private int _subSeed;
    private int _markerSubSeed;
    private int _lightingSubSeed;
    private int _dcScanSeed;

    // Enemy Respawning
    private bool _enemyRespawning;
    private bool _requireOtherZone = true;
    private int _roomDistance = 2;
    private double _timeInterval = 10.0;
    private double _countMultiplier = 1.0;
    private ObservableCollection<EnemyExcludeItem> _excludeList = new();

    // Clusters & Spawners & Pickups
    private int _hsuClusters;
    private int _corpseClusters;
    private int _resourceContainerClusters;
    private int _corpsesInZone;
    private ZoneDistributionAmount _groundSpawnersInZone = ZoneDistributionAmount.None_0;
    private ZoneDistributionAmount _hsusInZone = ZoneDistributionAmount.None_0;
    private ZoneDistributionAmount _deconUnitsInZone = ZoneDistributionAmount.None_0;
    private ObservableCollection<BigPickupDistributionItem> _bigPickupDistributionInZone = new();
    private ObservableCollection<DisinfectionStationPlacement> _disinfectionStationPlacements = new();
    private ObservableCollection<TerminalPlacement> _terminalPlacements = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ZoneSettings()
    {
        EditorDefaultsService.ApplyZoneSettingsDefaults(this);
    }

    public SectorType Sector
    {
        get => _sector;
        set => SetField(ref _sector, value);
    }

    public int SubSeed
    {
        get => _subSeed;
        set => SetField(ref _subSeed, value);
    }

    public int MarkerSubSeed
    {
        get => _markerSubSeed;
        set => SetField(ref _markerSubSeed, value);
    }

    public int LightingSubSeed
    {
        get => _lightingSubSeed;
        set => SetField(ref _lightingSubSeed, value);
    }

    public int DcScanSeed
    {
        get => _dcScanSeed;
        set => SetField(ref _dcScanSeed, value);
    }

    public bool EnemyRespawning
    {
        get => _enemyRespawning;
        set => SetField(ref _enemyRespawning, value);
    }

    public bool RequireOtherZone
    {
        get => _requireOtherZone;
        set => SetField(ref _requireOtherZone, value);
    }

    public int RoomDistance
    {
        get => _roomDistance;
        set => SetField(ref _roomDistance, value);
    }

    public double TimeInterval
    {
        get => _timeInterval;
        set => SetField(ref _timeInterval, value);
    }

    public double CountMultiplier
    {
        get => _countMultiplier;
        set => SetField(ref _countMultiplier, value);
    }

    public ObservableCollection<EnemyExcludeItem> ExcludeList
    {
        get => _excludeList;
        set => SetField(ref _excludeList, value ?? new ObservableCollection<EnemyExcludeItem>());
    }

    public int HSUClusters
    {
        get => _hsuClusters;
        set => SetField(ref _hsuClusters, Math.Max(0, value));
    }

    public int CorpseClusters
    {
        get => _corpseClusters;
        set => SetField(ref _corpseClusters, Math.Max(0, value));
    }

    public int ResourceContainerClusters
    {
        get => _resourceContainerClusters;
        set => SetField(ref _resourceContainerClusters, Math.Max(0, value));
    }

    public int CorpsesInZone
    {
        get => _corpsesInZone;
        set => SetField(ref _corpsesInZone, Math.Max(0, value));
    }

    public ZoneDistributionAmount GroundSpawnersInZone
    {
        get => _groundSpawnersInZone;
        set => SetField(ref _groundSpawnersInZone, value);
    }

    public ZoneDistributionAmount HSUsInZone
    {
        get => _hsusInZone;
        set => SetField(ref _hsusInZone, value);
    }

    public ZoneDistributionAmount DeconUnitsInZone
    {
        get => _deconUnitsInZone;
        set => SetField(ref _deconUnitsInZone, value);
    }

    public ObservableCollection<BigPickupDistributionItem> BigPickupDistributionInZone
    {
        get => _bigPickupDistributionInZone;
        set => SetField(ref _bigPickupDistributionInZone, value ?? new ObservableCollection<BigPickupDistributionItem>());
    }

    public ObservableCollection<DisinfectionStationPlacement> DisinfectionStationPlacements
    {
        get => _disinfectionStationPlacements;
        set => SetField(ref _disinfectionStationPlacements, value ?? new ObservableCollection<DisinfectionStationPlacement>());
    }

    public ObservableCollection<TerminalPlacement> TerminalPlacements
    {
        get => _terminalPlacements;
        set => SetField(ref _terminalPlacements, value ?? new ObservableCollection<TerminalPlacement>());
    }

    public int ZoneIndex
    {
        get => _zoneIndex;
        set => SetField(ref _zoneIndex, value);
    }

    public uint Lighting
    {
        get => _lighting;
        set => SetField(ref _lighting, value);
    }

    public uint ConsumableDistributionInZone
    {
        get => _consumableDistributionInZone;
        set => SetField(ref _consumableDistributionInZone, value);
    }

    public int MedKitUses
    {
        get => _medKitUses;
        set => SetField(ref _medKitUses, Math.Max(0, value));
    }

    public int AmmoKitUses
    {
        get => _ammoKitUses;
        set => SetField(ref _ammoKitUses, Math.Max(0, value));
    }

    public int ToolKitUses
    {
        get => _toolKitUses;
        set => SetField(ref _toolKitUses, Math.Max(0, value));
    }

    public int DisinfectKitUses
    {
        get => _disinfectKitUses;
        set => SetField(ref _disinfectKitUses, Math.Max(0, value));
    }

    public bool ContainsDC
    {
        get => _containsDC;
        set => SetField(ref _containsDC, value);
    }

    public bool DcOpensSecondary
    {
        get => _dcOpensSecondary;
        set => SetField(ref _dcOpensSecondary, value);
    }

    public bool DcOpensOverload
    {
        get => _dcOpensOverload;
        set => SetField(ref _dcOpensOverload, value);
    }

    public ObservableCollection<string> DcOpenedMainDoorIds
    {
        get => _dcOpenedMainDoorIds;
        set => SetField(ref _dcOpenedMainDoorIds, value ?? new ObservableCollection<string>());
    }

    public bool ContainsGenCluster
    {
        get => _containsGenCluster;
        set => SetField(ref _containsGenCluster, value);
    }

    public int PowerCells
    {
        get => _powerCells;
        set => SetField(ref _powerCells, Math.Max(0, value));
    }

    public int FogTurbines
    {
        get => _fogTurbines;
        set => SetField(ref _fogTurbines, Math.Max(0, value));
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

public class DcMainDoorOption : INotifyPropertyChanged
{
    private string _doorId = string.Empty;
    private string _displayName = string.Empty;
    private bool _isSelected;
    private PlacedDoorNode? _door;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DoorId
    {
        get => _doorId;
        set => SetField(ref _doorId, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public PlacedDoorNode? Door
    {
        get => _door;
        set => SetField(ref _door, value);
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
