using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Controls;

public class GradientBarControl : Control
{
    public static readonly StyledProperty<AppearanceSettings?> AppearanceProperty =
        AvaloniaProperty.Register<GradientBarControl, AppearanceSettings?>(nameof(Appearance));

    public AppearanceSettings? Appearance
    {
        get => GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    private const double HandleWidth = 14.0;
    private const double HandleHeight = 32.0;
    private const double TrackHeight = 24.0;
    private const double CornerRadius = 4.0;
    private const double HandleCornerRadius = 3.0;

    private GradientKeyframe? _draggingKeyframe;
    private bool _isDragging;
    private AppearanceSettings? _hookedAppearance;

    static GradientBarControl()
    {
        AffectsRender<GradientBarControl>(AppearanceProperty);
    }

    public GradientBarControl()
    {
        ClipToBounds = false;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AppearanceProperty)
        {
            UnhookAppearance(_hookedAppearance);
            HookAppearance(change.GetNewValue<AppearanceSettings?>());
            InvalidateVisual();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        HookAppearance(Appearance);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnhookAppearance(_hookedAppearance);
    }

    private void HookAppearance(AppearanceSettings? appearance)
    {
        if (appearance == null || appearance == _hookedAppearance) return;

        UnhookAppearance(_hookedAppearance);
        _hookedAppearance = appearance;

        _hookedAppearance.PropertyChanged += OnAppearancePropertyChanged;
        if (_hookedAppearance.HeightmapGradient != null)
        {
            _hookedAppearance.HeightmapGradient.CollectionChanged += OnGradientCollectionChanged;
            foreach (var kf in _hookedAppearance.HeightmapGradient)
            {
                kf.PropertyChanged += OnKeyframePropertyChanged;
            }
        }
    }

    private void UnhookAppearance(AppearanceSettings? appearance)
    {
        if (appearance == null) return;

        appearance.PropertyChanged -= OnAppearancePropertyChanged;
        if (appearance.HeightmapGradient != null)
        {
            appearance.HeightmapGradient.CollectionChanged -= OnGradientCollectionChanged;
            foreach (var kf in appearance.HeightmapGradient)
            {
                kf.PropertyChanged -= OnKeyframePropertyChanged;
            }
        }

        if (_hookedAppearance == appearance)
        {
            _hookedAppearance = null;
        }
    }

    private void OnAppearancePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppearanceSettings.HeightmapGradient))
        {
            if (_hookedAppearance?.HeightmapGradient != null)
            {
                _hookedAppearance.HeightmapGradient.CollectionChanged -= OnGradientCollectionChanged;
                _hookedAppearance.HeightmapGradient.CollectionChanged += OnGradientCollectionChanged;
            }
        }
        InvalidateVisual();
    }

    private void OnGradientCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (GradientKeyframe kf in e.NewItems)
            {
                kf.PropertyChanged += OnKeyframePropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (GradientKeyframe kf in e.OldItems)
            {
                kf.PropertyChanged -= OnKeyframePropertyChanged;
            }
        }
        InvalidateVisual();
    }

    private void OnKeyframePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        double trackY = (h - TrackHeight) / 2.0;
        var trackRect = new Rect(0, trackY, w, TrackHeight);

        // Draw track with gradient background
        var trackBrush = Appearance?.HeightmapGradientBrush ?? Brushes.DarkSlateGray;
        var trackPen = new Pen(new SolidColorBrush(Color.Parse("#444444")), 1.0);
        context.DrawRectangle(trackBrush, trackPen, new RoundedRect(trackRect, CornerRadius));

        if (Appearance?.HeightmapGradient == null || Appearance.HeightmapGradient.Count == 0)
            return;

        double effectiveWidth = Math.Max(0.0, w - HandleWidth);
        double handleY = (h - HandleHeight) / 2.0;

        var keyframes = Appearance.HeightmapGradient.ToList();
        var selected = Appearance.SelectedKeyframe;
        var unselected = keyframes.Where(k => k != selected).ToList();

        // Draw unselected handles first, then the selected handle on top
        foreach (var kf in unselected)
        {
            DrawHandle(context, kf, effectiveWidth, handleY, isSelected: false);
        }

        if (selected != null && keyframes.Contains(selected))
        {
            DrawHandle(context, selected, effectiveWidth, handleY, isSelected: true);
        }
    }

    private static void DrawHandle(DrawingContext context, GradientKeyframe kf, double effectiveWidth, double handleY, bool isSelected)
    {
        double x = Math.Clamp(kf.Position, 0.0, 1.0) * effectiveWidth;
        var handleRect = new Rect(x, handleY, HandleWidth, HandleHeight);
        var roundedRect = new RoundedRect(handleRect, HandleCornerRadius);

        // Fill with keyframe color
        var fillBrush = new SolidColorBrush(kf.Color);
        context.DrawRectangle(fillBrush, null, roundedRect);

        if (isSelected)
        {
            // Outer accent border
            var accentPen = new Pen(new SolidColorBrush(Color.Parse("#00A2FF")), 2.5);
            context.DrawRectangle(null, accentPen, roundedRect);

            // Inner contrast white border
            var innerRect = new Rect(x + 1.5, handleY + 1.5, Math.Max(1.0, HandleWidth - 3.0), Math.Max(1.0, HandleHeight - 3.0));
            var innerPen = new Pen(new SolidColorBrush(Color.Parse("#FFFFFF")), 1.0);
            context.DrawRectangle(null, innerPen, new RoundedRect(innerRect, Math.Max(1.0, HandleCornerRadius - 1.0)));
        }
        else
        {
            // Dark outer outline
            var outerPen = new Pen(new SolidColorBrush(Color.Parse("#1A1A1A")), 1.5);
            context.DrawRectangle(null, outerPen, roundedRect);

            // Light inner border
            var innerRect = new Rect(x + 0.5, handleY + 0.5, Math.Max(1.0, HandleWidth - 1.0), Math.Max(1.0, HandleHeight - 1.0));
            var borderPen = new Pen(new SolidColorBrush(Color.Parse("#E0E0E0")), 1.0);
            context.DrawRectangle(null, borderPen, new RoundedRect(innerRect, Math.Max(1.0, HandleCornerRadius - 0.5)));
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var currentPoint = e.GetCurrentPoint(this);
        if (!currentPoint.Properties.IsLeftButtonPressed || Appearance?.HeightmapGradient == null)
            return;

        var p = e.GetPosition(this);
        double effectiveWidth = Math.Max(1.0, Bounds.Width - HandleWidth);
        double handleY = (Bounds.Height - HandleHeight) / 2.0;

        var keyframes = Appearance.HeightmapGradient.ToList();
        GradientKeyframe? hit = null;

        // Check if selected keyframe was clicked first
        if (Appearance.SelectedKeyframe != null && keyframes.Contains(Appearance.SelectedKeyframe))
        {
            double x = Math.Clamp(Appearance.SelectedKeyframe.Position, 0.0, 1.0) * effectiveWidth;
            var r = new Rect(x - 2, handleY - 2, HandleWidth + 4, HandleHeight + 4);
            if (r.Contains(p))
            {
                hit = Appearance.SelectedKeyframe;
            }
        }

        // Check remaining keyframes in reverse order
        if (hit == null)
        {
            for (int i = keyframes.Count - 1; i >= 0; i--)
            {
                var kf = keyframes[i];
                double x = Math.Clamp(kf.Position, 0.0, 1.0) * effectiveWidth;
                var r = new Rect(x - 2, handleY - 2, HandleWidth + 4, HandleHeight + 4);
                if (r.Contains(p))
                {
                    hit = kf;
                    break;
                }
            }
        }

        if (hit != null)
        {
            Appearance.SelectedKeyframe = hit;
            _draggingKeyframe = hit;
            _isDragging = true;
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
        }
        else if (keyframes.Count > 0)
        {
            // Clicked on track: select closest keyframe
            var closest = keyframes.OrderBy(k => Math.Abs(k.Position * effectiveWidth + HandleWidth / 2.0 - p.X)).First();
            Appearance.SelectedKeyframe = closest;
            _draggingKeyframe = closest;
            _isDragging = true;
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isDragging && _draggingKeyframe != null && Appearance != null)
        {
            var p = e.GetPosition(this);
            double effectiveWidth = Math.Max(1.0, Bounds.Width - HandleWidth);
            double newPos = Math.Clamp((p.X - HandleWidth / 2.0) / effectiveWidth, 0.0, 1.0);
            _draggingKeyframe.Position = Math.Round(newPos, 3);
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isDragging)
        {
            _isDragging = false;
            _draggingKeyframe = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        _isDragging = false;
        _draggingKeyframe = null;
        InvalidateVisual();
    }
}
