using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace DuneEdit.Desktop.Views;

public partial class MainWindow : Window
{
    private const double MinimumMapZoom = 1.0;
    private const double MaximumMapZoom = 4.0;
    private const double MouseWheelZoomFactor = 1.15;
    private const double WheelPanDistance = 50.0;

    private readonly MatrixTransform mapTransform = new();
    private double mapZoom = MinimumMapZoom;
    private Vector mapTranslation;
    private IPointer? dragPointer;
    private Point dragStart;
    private Vector dragStartTranslation;
    private bool isPinching;
    private double pinchStartZoom;

    public MainWindow()
    {
        InitializeComponent();

        MapContent.RenderTransform = mapTransform;
        ApplyMapTransform();

        MapViewport.PointerPressed += MapPointerPressed;
        MapViewport.PointerMoved += MapPointerMoved;
        MapViewport.PointerReleased += MapPointerReleased;
        MapViewport.PointerCaptureLost += MapPointerCaptureLost;
        MapViewport.PointerWheelChanged += MapPointerWheelChanged;
        MapViewport.PointerTouchPadGestureMagnify += MapTouchPadMagnified;
        MapViewport.Pinch += MapPinched;
        MapViewport.PinchEnded += MapPinchEnded;
        MapViewport.ScrollGesture += MapScrolled;
        MapViewport.SizeChanged += MapViewportSizeChanged;
    }

    private void MapPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (mapZoom <= MinimumMapZoom
            || IsOverButton(e.Source)
            || !e.GetCurrentPoint(MapViewport).Properties.IsLeftButtonPressed)
        {
            return;
        }

        dragPointer = e.Pointer;
        dragStart = e.GetPosition(MapViewport);
        dragStartTranslation = mapTranslation;
        e.Pointer.Capture(MapViewport);
        e.Handled = true;
    }

    private void MapPointerMoved(object? sender, PointerEventArgs e)
    {
        if (dragPointer != e.Pointer)
        {
            return;
        }

        var delta = e.GetPosition(MapViewport) - dragStart;
        SetMapTranslation(dragStartTranslation + delta);
        e.Handled = true;
    }

    private void MapPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (dragPointer != e.Pointer)
        {
            return;
        }

        dragPointer = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void MapPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (dragPointer == e.Pointer)
        {
            dragPointer = null;
        }
    }

    private void MapPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0)
        {
            if (e.Delta.Y != 0)
            {
                var zoomFactor = Math.Pow(MouseWheelZoomFactor, e.Delta.Y);
                SetMapZoom(mapZoom * zoomFactor, e.GetPosition(MapViewport));
            }

            e.Handled = true;
            return;
        }

        var delta = e.Delta;
        if ((e.KeyModifiers & KeyModifiers.Shift) != 0 && delta.X == 0)
        {
            delta = new Vector(delta.Y, 0);
        }

        if (SetMapTranslation(mapTranslation + (delta * WheelPanDistance)))
        {
            e.Handled = true;
        }
    }

    private void MapTouchPadMagnified(object? sender, PointerDeltaEventArgs e)
    {
        SetMapZoom(
            mapZoom * Math.Exp(e.Delta.Y),
            e.GetPosition(MapViewport));
        e.Handled = true;
    }

    private void MapPinched(object? sender, PinchEventArgs e)
    {
        if (!isPinching)
        {
            isPinching = true;
            pinchStartZoom = mapZoom;
        }

        SetMapZoom(pinchStartZoom * e.Scale, e.ScaleOrigin);
        e.Handled = true;
    }

    private void MapScrolled(object? sender, ScrollGestureEventArgs e)
    {
        if (mapZoom > MinimumMapZoom)
        {
            SetMapTranslation(mapTranslation - e.Delta);
            e.Handled = true;
        }
    }

    private void MapPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        isPinching = false;
        e.Handled = true;
    }

    private void MapViewportSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        SetMapTranslation(mapTranslation);
    }

    private void SetMapZoom(double requestedZoom, Point origin)
    {
        var nextZoom = Math.Clamp(requestedZoom, MinimumMapZoom, MaximumMapZoom);
        if (Math.Abs(nextZoom - mapZoom) < double.Epsilon)
        {
            return;
        }

        var contentPoint = new Point(
            (origin.X - mapTranslation.X) / mapZoom,
            (origin.Y - mapTranslation.Y) / mapZoom);

        mapZoom = nextZoom;
        mapTranslation = ClampMapTranslation(
            new Vector(
                origin.X - (contentPoint.X * mapZoom),
                origin.Y - (contentPoint.Y * mapZoom)));
        ApplyMapTransform();
    }

    private bool SetMapTranslation(Vector requestedTranslation)
    {
        var nextTranslation = ClampMapTranslation(requestedTranslation);
        if (nextTranslation == mapTranslation)
        {
            return false;
        }

        mapTranslation = nextTranslation;
        ApplyMapTransform();
        return true;
    }

    private Vector ClampMapTranslation(Vector translation)
    {
        var viewport = MapViewport.Bounds.Size;
        var minimumX = Math.Min(0, viewport.Width * (1 - mapZoom));
        var minimumY = Math.Min(0, viewport.Height * (1 - mapZoom));

        return new Vector(
            Math.Clamp(translation.X, minimumX, 0),
            Math.Clamp(translation.Y, minimumY, 0));
    }

    private void ApplyMapTransform()
    {
        mapTransform.Matrix = new Matrix(
            mapZoom,
            0,
            0,
            mapZoom,
            mapTranslation.X,
            mapTranslation.Y);
        MapZoomText.Text = $"{mapZoom * 100:0}%";
    }

    private static bool IsOverButton(object? source)
    {
        for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is Button)
            {
                return true;
            }
        }

        return false;
    }
}