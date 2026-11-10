using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Avalonia;

namespace GTFO_GUIEditor.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TriggerType
{
    Collision,
    Interaction,
    LookAt
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CarryItemInsertType
{
    None,
    PowerCell,
    SmallHSU,
    Crate
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ItemStateAfterInsert
{
    Default,
    Inserted_Visible_NotInteractable,
    Inserted_Disabled
}

public class CarryItemTransformData : INotifyPropertyChanged
{
    private Vector3Data _position = new();
    private Vector3Data _rotation = new();
    private Vector3Data _scale = new(1.0, 1.0, 1.0);

    public event PropertyChangedEventHandler? PropertyChanged;

    public CarryItemTransformData()
    {
        _position.PropertyChanged += (s, e) => OnPositionChanged(s, e);
        _rotation.PropertyChanged += (s, e) => OnRotationChanged(s, e);
        _scale.PropertyChanged += (s, e) => OnScaleChanged(s, e);
    }

    [JsonPropertyName("position")]
    public Vector3Data Position
    {
        get => _position;
        set
        {
            var old = _position;
            if (SetField(ref _position, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= OnPositionChanged;
                _position.PropertyChanged += OnPositionChanged;
            }
        }
    }

    [JsonPropertyName("rotation")]
    public Vector3Data Rotation
    {
        get => _rotation;
        set
        {
            var old = _rotation;
            if (SetField(ref _rotation, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= OnRotationChanged;
                _rotation.PropertyChanged += OnRotationChanged;
            }
        }
    }

    [JsonPropertyName("scale")]
    public Vector3Data Scale
    {
        get => _scale;
        set
        {
            var old = _scale;
            if (SetField(ref _scale, value ?? new Vector3Data(1.0, 1.0, 1.0)))
            {
                if (old != null) old.PropertyChanged -= OnScaleChanged;
                _scale.PropertyChanged += OnScaleChanged;
            }
        }
    }

    private void OnPositionChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Position));
    private void OnRotationChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Rotation));
    private void OnScaleChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Scale));

    public CarryItemTransformData Clone()
    {
        return new CarryItemTransformData
        {
            Position = Position.Clone(),
            Rotation = Rotation.Clone(),
            Scale = Scale.Clone()
        };
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

public enum TriggerShape
{
    Box,
    Sphere,
    Capsule
}

public class PlacedTriggerNode : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "Trigger";
    private TriggerShape _shape = TriggerShape.Box;
    private TriggerType _type = TriggerType.Collision;
    private bool _isToggle;
    private bool _isWorldEventObject;
    private PaletteItemType _itemType = PaletteItemType.TriggerBox;
    private ARA_CustomWorldEventObjectOptions _pickupToSpawn = ARA_CustomWorldEventObjectOptions.DataSphere;
    private Vector3Data _center = new();
    private Vector3Data _rotation = new();
    private Vector3Data _scale = new(1.0, 1.0, 1.0);
    private int _instanceIndex;
    private int _areaIndex = -1;
    private bool _useExistingFilterInArea = false;
    private bool _useRandomPosition;
    private Vector3Data _size = new(10.0, 10.0, 10.0);
    private double _radius = 5.0;
    private double _height = 10.0;
    private bool _isHovered;
    private string _interactionText = string.Empty;
    private CarryItemInsertType _carryItemInsertType = CarryItemInsertType.None;
    private CarryItemTransformData _carryItemTransform = new();
    private bool _removeItemOnInsert;
    private ItemStateAfterInsert _itemStateAfterInsert = ItemStateAfterInsert.Default;
    private ObservableCollection<WardenEvent> _events = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public PlacedTriggerNode()
    {
        _center.PropertyChanged += (s, e) => NotifyTransformProperties();
        _size.PropertyChanged += (s, e) => NotifyTransformProperties();
        _rotation.PropertyChanged += (s, e) => NotifyTransformProperties();
        _scale.PropertyChanged += (s, e) => NotifyTransformProperties();
        _carryItemTransform.PropertyChanged += OnCarryItemTransformChanged;
    }

    [JsonPropertyName("id")]
    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    [JsonPropertyName("shape")]
    public TriggerShape Shape
    {
        get => _shape;
        set
        {
            if (SetField(ref _shape, value))
            {
                OnPropertyChanged(nameof(IsBox));
                OnPropertyChanged(nameof(IsSphere));
                OnPropertyChanged(nameof(IsCapsule));
                OnPropertyChanged(nameof(ShapeDisplayName));
                OnPropertyChanged(nameof(ShapeIcon));
                NotifyTransformProperties();
            }
        }
    }

    [JsonPropertyName("isWorldEventObject")]
    public bool IsWorldEventObject
    {
        get => _isWorldEventObject;
        set
        {
            if (SetField(ref _isWorldEventObject, value))
            {
                if (_isWorldEventObject)
                {
                    _areaIndex = -1;
                    _useExistingFilterInArea = false;
                    if (_name == "Trigger" || string.IsNullOrWhiteSpace(_name))
                    {
                        _name = $"BigPickup_{_pickupToSpawn}";
                    }
                }

                OnPropertyChanged(nameof(IsBigPickup));
                OnPropertyChanged(nameof(ShapeDisplayName));
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(TooltipText));
                OnPropertyChanged(nameof(IsBox));
                OnPropertyChanged(nameof(IsSphere));
                OnPropertyChanged(nameof(IsCapsule));
                OnPropertyChanged(nameof(IsCollision));
                OnPropertyChanged(nameof(IsInteraction));
                OnPropertyChanged(nameof(IsLookAt));
                OnPropertyChanged(nameof(HasCarryItem));
                NotifyTransformProperties();
            }
        }
    }

    [JsonPropertyName("itemType")]
    public PaletteItemType ItemType
    {
        get => _itemType;
        set
        {
            if (SetField(ref _itemType, value))
            {
                if (_itemType == PaletteItemType.BigPickup)
                {
                    _areaIndex = -1;
                    _useExistingFilterInArea = false;
                    if (_name == "Trigger" || string.IsNullOrWhiteSpace(_name))
                    {
                        _name = $"BigPickup_{_pickupToSpawn}";
                    }
                }

                OnPropertyChanged(nameof(IsBigPickup));
                OnPropertyChanged(nameof(ShapeDisplayName));
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(TooltipText));
                OnPropertyChanged(nameof(IsBox));
                OnPropertyChanged(nameof(IsSphere));
                OnPropertyChanged(nameof(IsCapsule));
                OnPropertyChanged(nameof(IsCollision));
                OnPropertyChanged(nameof(IsInteraction));
                OnPropertyChanged(nameof(IsLookAt));
                OnPropertyChanged(nameof(HasCarryItem));
                NotifyTransformProperties();
            }
        }
    }

    [JsonPropertyName("pickupToSpawn")]
    public ARA_CustomWorldEventObjectOptions PickupToSpawn
    {
        get => _pickupToSpawn;
        set
        {
            if (SetField(ref _pickupToSpawn, value))
            {
                if (IsBigPickup && (_name.StartsWith("BigPickup_") || _name == "Trigger" ||
                                    string.IsNullOrWhiteSpace(_name)))
                {
                    _name = $"BigPickup_{_pickupToSpawn}";
                }

                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(ShapeDisplayName));
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    [JsonPropertyName("rotation")]
    public Vector3Data Rotation
    {
        get => _rotation;
        set
        {
            var old = _rotation;
            if (SetField(ref _rotation, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= OnRotationPropertyChanged;
                _rotation.PropertyChanged += OnRotationPropertyChanged;
                NotifyTransformProperties();
            }
        }
    }

    [JsonPropertyName("scale")]
    public Vector3Data Scale
    {
        get => _scale;
        set
        {
            var old = _scale;
            if (SetField(ref _scale, value ?? new Vector3Data(1.0, 1.0, 1.0)))
            {
                if (old != null) old.PropertyChanged -= OnScalePropertyChanged;
                _scale.PropertyChanged += OnScalePropertyChanged;
                NotifyTransformProperties();
            }
        }
    }

    [JsonPropertyName("instanceIndex")]
    public int InstanceIndex
    {
        get => _instanceIndex;
        set => SetField(ref _instanceIndex, value);
    }

    [JsonPropertyName("areaIndex")]
    public int AreaIndex
    {
        get => _areaIndex;
        set => SetField(ref _areaIndex, value);
    }

    [JsonPropertyName("useExistingFilterInArea")]
    public bool UseExistingFilterInArea
    {
        get => _useExistingFilterInArea;
        set => SetField(ref _useExistingFilterInArea, value);
    }

    [JsonPropertyName("useRandomPosition")]
    public bool UseRandomPosition
    {
        get => _useRandomPosition;
        set => SetField(ref _useRandomPosition, value);
    }

    [JsonIgnore] public bool IsBigPickup => IsWorldEventObject || ItemType == PaletteItemType.BigPickup;

    [JsonPropertyName("center")]
    public Vector3Data Center
    {
        get => _center;
        set
        {
            var old = _center;
            if (SetField(ref _center, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= OnCenterPropertyChanged;
                _center.PropertyChanged += OnCenterPropertyChanged;
                NotifyTransformProperties();
            }
        }
    }

    [JsonPropertyName("size")]
    public Vector3Data Size
    {
        get => _size;
        set
        {
            var old = _size;
            if (SetField(ref _size, value ?? new Vector3Data(10.0, 10.0, 10.0)))
            {
                if (old != null) old.PropertyChanged -= OnSizePropertyChanged;
                _size.PropertyChanged += OnSizePropertyChanged;
                NotifyTransformProperties();
            }
        }
    }

    [JsonPropertyName("radius")]
    public double Radius
    {
        get => _radius;
        set
        {
            if (SetField(ref _radius, Math.Max(0.1, value)))
            {
                NotifyTransformProperties();
            }
        }
    }

    [JsonPropertyName("height")]
    public double Height
    {
        get => _height;
        set
        {
            if (SetField(ref _height, Math.Max(0.1, value)))
            {
                NotifyTransformProperties();
            }
        }
    }

    [JsonPropertyName("type")]
    public TriggerType Type
    {
        get => _type;
        set
        {
            if (SetField(ref _type, value))
            {
                OnPropertyChanged(nameof(IsCollision));
                OnPropertyChanged(nameof(IsInteraction));
                OnPropertyChanged(nameof(IsLookAt));
                OnPropertyChanged(nameof(TooltipText));
            }
        }
    }

    [JsonPropertyName("isToggle")]
    public bool IsToggle
    {
        get => _isToggle;
        set => SetField(ref _isToggle, value);
    }

    [JsonPropertyName("interactionText")]
    public string InteractionText
    {
        get => _interactionText;
        set => SetField(ref _interactionText, value);
    }

    [JsonPropertyName("carryItemInsertType")]
    public CarryItemInsertType CarryItemInsertType
    {
        get => _carryItemInsertType;
        set
        {
            if (SetField(ref _carryItemInsertType, value))
            {
                OnPropertyChanged(nameof(HasCarryItem));
            }
        }
    }

    [JsonPropertyName("carryItemTransform")]
    public CarryItemTransformData CarryItemTransform
    {
        get => _carryItemTransform;
        set
        {
            var old = _carryItemTransform;
            if (SetField(ref _carryItemTransform, value ?? new CarryItemTransformData()))
            {
                if (old != null) old.PropertyChanged -= OnCarryItemTransformChanged;
                _carryItemTransform.PropertyChanged += OnCarryItemTransformChanged;
                OnPropertyChanged(nameof(CarryItemTransform));
            }
        }
    }

    [JsonPropertyName("removeItemOnInsert")]
    public bool RemoveItemOnInsert
    {
        get => _removeItemOnInsert;
        set => SetField(ref _removeItemOnInsert, value);
    }

    [JsonPropertyName("itemStateAfterInsert")]
    public ItemStateAfterInsert ItemStateAfterInsert
    {
        get => _itemStateAfterInsert;
        set => SetField(ref _itemStateAfterInsert, value);
    }

    [JsonPropertyName("events")]
    public ObservableCollection<WardenEvent> Events
    {
        get => _events;
        set => SetField(ref _events, value ?? new ObservableCollection<WardenEvent>());
    }

    public void AddEvent() => Events.Add(new WardenEvent());

    public void RemoveEvent(WardenEvent? ev)
    {
        if (ev != null) Events.Remove(ev);
    }

    [JsonIgnore] public bool IsCollision => !IsBigPickup && Type == TriggerType.Collision;

    [JsonIgnore] public bool IsInteraction => !IsBigPickup && Type == TriggerType.Interaction;

    [JsonIgnore] public bool IsLookAt => !IsBigPickup && Type == TriggerType.LookAt;

    [JsonIgnore]
    public bool HasCarryItem => !IsBigPickup && IsInteraction && CarryItemInsertType != CarryItemInsertType.None;

    [JsonIgnore]
    public bool IsHovered
    {
        get => _isHovered;
        set => SetField(ref _isHovered, value);
    }

    [JsonIgnore] public bool IsBox => !IsBigPickup && Shape == TriggerShape.Box;

    [JsonIgnore] public bool IsSphere => !IsBigPickup && Shape == TriggerShape.Sphere;

    [JsonIgnore] public bool IsCapsule => !IsBigPickup && Shape == TriggerShape.Capsule;

    [JsonIgnore]
    public string ShapeDisplayName => IsBigPickup
        ? $"Big Pickup ({PickupToSpawn})"
        : Shape switch
        {
            TriggerShape.Box => "Box Collider",
            TriggerShape.Sphere => "Sphere Collider",
            TriggerShape.Capsule => "Capsule Collider",
            _ => "Trigger"
        };

    [JsonIgnore] public string ShapeIcon => string.Empty;

    [JsonIgnore] public string DisplayName => string.IsNullOrWhiteSpace(Name) ? ShapeDisplayName : Name;

    // Canvas coordinate calculations:
    // Center of elevator tile is (0,0) in item space -> (32, 32) in canvas world space
    // Top-right of elevator tile is (32, 32) in item space -> (64, 0) in canvas world space
    [JsonIgnore] public double WorldCenterX => Center.X + 32.0;

    [JsonIgnore] public double WorldCenterY => 32.0 - Center.Y;

    [JsonIgnore]
    public double CanvasWidth => IsBigPickup
        ? Math.Max(3.0, 3.0 * Scale.X)
        : Shape switch
        {
            TriggerShape.Box => Math.Max(4.0, Size.X),
            TriggerShape.Sphere => Math.Max(4.0, Radius * 2.0),
            TriggerShape.Capsule => Math.Max(4.0, Radius * 2.0),
            _ => 10.0
        };

    [JsonIgnore]
    public double CanvasHeight => IsBigPickup
        ? Math.Max(3.0, 3.0 * Scale.Y)
        : Shape switch
        {
            TriggerShape.Box => Math.Max(4.0, Size.Y),
            TriggerShape.Sphere => Math.Max(4.0, Radius * 2.0),
            TriggerShape.Capsule => Math.Max(4.0, Radius * 2.0),
            _ => 10.0
        };

    [JsonIgnore] public double CanvasLeft => WorldCenterX - (CanvasWidth / 2.0);

    [JsonIgnore] public double CanvasTop => WorldCenterY - (CanvasHeight / 2.0);

    [JsonIgnore]
    public string FillBrush => IsBigPickup
        ? "#4000E5FF"
        : Shape switch
        {
            TriggerShape.Box => "#40FFA726",
            TriggerShape.Sphere => "#4081C784",
            TriggerShape.Capsule => "#40BA68C8",
            _ => "#40FF9800"
        };

    [JsonIgnore]
    public string StrokeBrush => IsBigPickup
        ? "#FF00E5FF"
        : Shape switch
        {
            TriggerShape.Box => "#FFFFA726",
            TriggerShape.Sphere => "#FF81C784",
            TriggerShape.Capsule => "#FFBA68C8",
            _ => "#FFFF9800"
        };

    [JsonIgnore]
    public string ShapeColorHex => IsBigPickup
        ? "#00E5FF"
        : Shape switch
        {
            TriggerShape.Box => "#FFA726",
            TriggerShape.Sphere => "#81C784",
            TriggerShape.Capsule => "#BA68C8",
            _ => "#FF9800"
        };

    [JsonIgnore]
    public CornerRadius CanvasCornerRadius => IsBigPickup
        ? new CornerRadius(2)
        : Shape switch
        {
            TriggerShape.Box => new CornerRadius(2),
            TriggerShape.Sphere => new CornerRadius(999),
            TriggerShape.Capsule => new CornerRadius(999),
            _ => new CornerRadius(2)
        };

    [JsonIgnore]
    public string TooltipText
    {
        get
        {
            if (IsBigPickup)
            {
                return
                    $"Big Pickup: {PickupToSpawn}\nFilter: {DisplayName}\nPosition: ({Center.X:F2}, {Center.Y:F2}, {Center.Z:F2})\nRotation: ({Rotation.X:F1}, {Rotation.Y:F1}, {Rotation.Z:F1})\nScale: ({Scale.X:F1}, {Scale.Y:F1}, {Scale.Z:F1})";
            }

            var shapeDetails = Shape switch
            {
                TriggerShape.Box => $"Size: X={Size.X:F1}, Y={Size.Y:F1}, Z={Size.Z:F1}",
                TriggerShape.Sphere => $"Radius: {Radius:F1} (Diameter: {Radius * 2.0:F1})",
                TriggerShape.Capsule => $"Radius: {Radius:F1}, Height: {Height:F1}",
                _ => string.Empty
            };
            return $"{ShapeDisplayName}\nPosition: ({Center.X:F2}, {Center.Y:F2}, {Center.Z:F2})\n{shapeDetails}";
        }
    }

    private void OnCenterPropertyChanged(object? sender, PropertyChangedEventArgs e) => NotifyTransformProperties();
    private void OnSizePropertyChanged(object? sender, PropertyChangedEventArgs e) => NotifyTransformProperties();
    private void OnRotationPropertyChanged(object? sender, PropertyChangedEventArgs e) => NotifyTransformProperties();
    private void OnScalePropertyChanged(object? sender, PropertyChangedEventArgs e) => NotifyTransformProperties();

    private void OnCarryItemTransformChanged(object? sender, PropertyChangedEventArgs e) =>
        OnPropertyChanged(nameof(CarryItemTransform));

    public void NotifyTransformProperties()
    {
        OnPropertyChanged(nameof(WorldCenterX));
        OnPropertyChanged(nameof(WorldCenterY));
        OnPropertyChanged(nameof(CanvasWidth));
        OnPropertyChanged(nameof(CanvasHeight));
        OnPropertyChanged(nameof(CanvasLeft));
        OnPropertyChanged(nameof(CanvasTop));
        OnPropertyChanged(nameof(FillBrush));
        OnPropertyChanged(nameof(StrokeBrush));
        OnPropertyChanged(nameof(ShapeColorHex));
        OnPropertyChanged(nameof(CanvasCornerRadius));
        OnPropertyChanged(nameof(TooltipText));
    }

    public PlacedTriggerNode Clone()
    {
        return new PlacedTriggerNode
        {
            Id = Id,
            Name = Name,
            Shape = Shape,
            Type = Type,
            IsToggle = IsToggle,
            IsWorldEventObject = IsWorldEventObject,
            ItemType = ItemType,
            PickupToSpawn = PickupToSpawn,
            Rotation = Rotation.Clone(),
            Scale = Scale.Clone(),
            InstanceIndex = InstanceIndex,
            AreaIndex = AreaIndex,
            UseExistingFilterInArea = UseExistingFilterInArea,
            UseRandomPosition = UseRandomPosition,
            Center = Center.Clone(),
            Size = Size.Clone(),
            Radius = Radius,
            Height = Height,
            InteractionText = InteractionText,
            CarryItemInsertType = CarryItemInsertType,
            CarryItemTransform = CarryItemTransform.Clone(),
            RemoveItemOnInsert = RemoveItemOnInsert,
            ItemStateAfterInsert = ItemStateAfterInsert,
            Events = new ObservableCollection<WardenEvent>(Events.Select(e => e.Clone()))
        };
    }

    public PlacedTriggerNodeData ToData()
    {
        return new PlacedTriggerNodeData
        {
            Id = Id,
            Name = Name,
            Shape = Shape,
            Type = Type,
            IsToggle = IsToggle,
            IsWorldEventObject = IsWorldEventObject,
            ItemType = ItemType,
            PickupToSpawn = PickupToSpawn,
            Rotation = Rotation.Clone(),
            Scale = Scale.Clone(),
            InstanceIndex = InstanceIndex,
            AreaIndex = AreaIndex,
            UseExistingFilterInArea = UseExistingFilterInArea,
            UseRandomPosition = UseRandomPosition,
            Center = Center.Clone(),
            Size = Size.Clone(),
            Radius = Radius,
            Height = Height,
            InteractionText = InteractionText,
            CarryItemInsertType = CarryItemInsertType,
            CarryItemTransform = CarryItemTransform.Clone(),
            RemoveItemOnInsert = RemoveItemOnInsert,
            ItemStateAfterInsert = ItemStateAfterInsert,
            Events = Events.Select(e => e.Clone()).ToList()
        };
    }

    public static PlacedTriggerNode FromData(PlacedTriggerNodeData data)
    {
        return new PlacedTriggerNode
        {
            Id = data.Id ?? Guid.NewGuid().ToString("N"),
            Name = data.Name ?? "Trigger",
            Shape = data.Shape,
            Type = data.Type,
            IsToggle = data.IsToggle,
            IsWorldEventObject = data.IsWorldEventObject,
            ItemType = data.ItemType,
            PickupToSpawn = data.PickupToSpawn,
            Rotation = data.Rotation?.Clone() ?? new Vector3Data(),
            Scale = data.Scale?.Clone() ?? new Vector3Data(1.0, 1.0, 1.0),
            InstanceIndex = data.InstanceIndex,
            AreaIndex = data.AreaIndex,
            UseExistingFilterInArea = data.UseExistingFilterInArea,
            UseRandomPosition = data.UseRandomPosition,
            Center = data.Center?.Clone() ?? new Vector3Data(),
            Size = data.Size?.Clone() ?? new Vector3Data(10.0, 10.0, 10.0),
            Radius = data.Radius > 0 ? data.Radius : 5.0,
            Height = data.Height > 0 ? data.Height : 10.0,
            InteractionText = data.InteractionText ?? string.Empty,
            CarryItemInsertType = data.CarryItemInsertType,
            CarryItemTransform = data.CarryItemTransform?.Clone() ?? new CarryItemTransformData(),
            RemoveItemOnInsert = data.RemoveItemOnInsert,
            ItemStateAfterInsert = data.ItemStateAfterInsert,
            Events = data.Events != null
                ? new ObservableCollection<WardenEvent>(data.Events.Select(e => e.Clone()))
                : new ObservableCollection<WardenEvent>()
        };
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

public class PlacedTriggerNodeData
{
    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("shape")] public TriggerShape Shape { get; set; }

    [JsonPropertyName("type")] public TriggerType Type { get; set; } = TriggerType.Collision;

    [JsonPropertyName("isToggle")] public bool IsToggle { get; set; }

    [JsonPropertyName("isWorldEventObject")]
    public bool IsWorldEventObject { get; set; }

    [JsonPropertyName("itemType")] public PaletteItemType ItemType { get; set; } = PaletteItemType.TriggerBox;

    [JsonPropertyName("pickupToSpawn")]
    public ARA_CustomWorldEventObjectOptions PickupToSpawn { get; set; } = ARA_CustomWorldEventObjectOptions.DataSphere;

    [JsonPropertyName("rotation")] public Vector3Data? Rotation { get; set; }

    [JsonPropertyName("scale")] public Vector3Data? Scale { get; set; }

    [JsonPropertyName("instanceIndex")] public int InstanceIndex { get; set; }

    [JsonPropertyName("areaIndex")] public int AreaIndex { get; set; } = -1;

    [JsonPropertyName("useExistingFilterInArea")]
    public bool UseExistingFilterInArea { get; set; } = false;

    [JsonPropertyName("useRandomPosition")]
    public bool UseRandomPosition { get; set; }

    [JsonPropertyName("center")] public Vector3Data? Center { get; set; }

    [JsonPropertyName("size")] public Vector3Data? Size { get; set; }

    [JsonPropertyName("radius")] public double Radius { get; set; } = 5.0;

    [JsonPropertyName("height")] public double Height { get; set; } = 10.0;

    [JsonPropertyName("interactionText")] public string? InteractionText { get; set; }

    [JsonPropertyName("carryItemInsertType")]
    public CarryItemInsertType CarryItemInsertType { get; set; } = CarryItemInsertType.None;

    [JsonPropertyName("carryItemTransform")]
    public CarryItemTransformData? CarryItemTransform { get; set; }

    [JsonPropertyName("removeItemOnInsert")]
    public bool RemoveItemOnInsert { get; set; }

    [JsonPropertyName("itemStateAfterInsert")]
    public ItemStateAfterInsert ItemStateAfterInsert { get; set; } = ItemStateAfterInsert.Default;

    [JsonPropertyName("events")] public List<WardenEvent>? Events { get; set; } = new();
}

public enum PaletteItemType
{
    TriggerBox,
    TriggerSphere,
    TriggerCapsule,
    BigPickup
}

public class LayoutPaletteItem : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _category = string.Empty;
    private string _displayName = string.Empty;
    private string _description = string.Empty;
    private PaletteItemType _itemType;
    private TriggerShape? _triggerShape;
    private string _iconText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Category
    {
        get => _category;
        set => SetField(ref _category, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    public PaletteItemType ItemType
    {
        get => _itemType;
        set => SetField(ref _itemType, value);
    }

    public TriggerShape? TriggerShape
    {
        get => _triggerShape;
        set => SetField(ref _triggerShape, value);
    }

    public string IconText
    {
        get => _iconText;
        set => SetField(ref _iconText, value);
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

public class LayoutPaletteCategory : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _isExpanded = true;
    private ObservableCollection<LayoutPaletteItem> _items = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }

    public ObservableCollection<LayoutPaletteItem> Items
    {
        get => _items;
        set => SetField(ref _items, value);
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