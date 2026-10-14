using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTFO_GUIEditor.Models;

public class ZoneSettings : INotifyPropertyChanged
{
    private int _zoneIndex;
    private uint _lighting;
    private int _medKitUses;
    private int _ammoKitUses;
    private int _toolKitUses;
    private int _disinfectKitUses;
    private int _bulkheadKeys;
    private bool _containsDC;
    private bool _dcOpensSecondary;
    private bool _dcOpensOverload;
    private ObservableCollection<string> _dcOpenedMainDoorIds = new();
    private bool _containsDisinfectStation;
    private int _powerCells;
    private int _fogTurbines;

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public int BulkheadKeys
    {
        get => _bulkheadKeys;
        set => SetField(ref _bulkheadKeys, Math.Max(0, value));
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

    public bool ContainsDisinfectStation
    {
        get => _containsDisinfectStation;
        set => SetField(ref _containsDisinfectStation, value);
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
